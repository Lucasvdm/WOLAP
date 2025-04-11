using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Archipelago.MultiClient.Net.Models;
using HarmonyLib;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;
using static ClockState;

namespace WOLAP
{
    [HarmonyPatch]
    internal class ScriptCommandPatches
    {
        [HarmonyPatch(typeof(MCommand), "StringToOp")]
        [HarmonyPostfix]
        static void StringToOpPatch(string s, ref MCommand.Op __result)
        {
            //Unused MCommand.Op values are limited and patching around the enum would be complicated, so adding this one base command whose first argument specifies the actual command to run
            if (s == "customcommand") __result = MCommand.Op.STATESHARE; //Unused Stadia-exclusive command
        }

        [HarmonyPatch(typeof(MCommand), "Execute")]
        [HarmonyPrefix]
        static bool ExecutePatch(MCommand __instance, ref bool __result, Action<MCommand> callback)
        {
            switch(__instance.op)
            {
                case MCommand.Op.RUNSCRIPT:
                    if (__instance.argCount == 2)
                    {
                        MEvalContext ectx = MEvalContext.instance;
                        ScriptContext sctx = new ScriptContext(__instance.StrArg(0), ectx);
                        sctx.RunState(__instance.StrArg(1));

                        __result = false;

                        Traverse traverse = new Traverse(__instance);
                        traverse.Method("UnresolveArgs").GetValue();

                        return false;
                    }
                    break;
                case MCommand.Op.ADDPERMAFLAG:
                case MCommand.Op.INCPERMAFLAG:
                case MCommand.Op.DELPERMAFLAG:
                    if (WolapPlugin.ModDataLoaded) return false; //Disabling these to prevent messing with permanent progression flags/stats during modded play
                    break;
                case MCommand.Op.STATESHARE: //customcommand
                    //Could/should maybe use the mod logger instead, but this is consistent with other commands and should show in the log/debug window anyway
                    if (__instance.argCount == 0) __instance.LogError("needs at least a custom command to run, got no arguments.");
                    else
                    {
                        if (callback != null)
                        {
                            callback(__instance);
                        }
                    }
                    break;
            }

            return true;
        }

        [HarmonyPatch(typeof(Dialog), "OnCommand")]
        [HarmonyPostfix]
        static void OnCommandDialogPatch(Dialog __instance, MEvalContext ectx, MCommand cmd, ref bool __result)
        {
            if (cmd.op == MCommand.Op.STATESHARE) //customcommand
            {
                //For simplicity's sake in the handler methods, remove the command name argument and leave only that command's actual args so they can be treated the same as any other command
                string cmdName = cmd.StrArg(0);
                string cmdArgs = cmd.StrArgRest(1);
                cmd.ClearArgs();
                foreach (string arg in cmdArgs.Split(','))
                {
                    cmd.AddArg(arg);
                }

                switch (cmdName)
                {
                    case "checklocation":
                        HandleCheckLocationCommand(cmd, __instance);
                        break;
                    case "addnpcstorecheck":
                        HandleAddNPCStoreCheckCommand(cmd);
                        break;
                    case "misspoint":
                        HandleMissedCheckCommand(cmd);
                        break;
                }

                __result = true; //Usually true by default, gets set to false by some dialog-closing commands or errors, but most Ops skip an assignment to false at the end of the method that will get caught before this patch
            }
        }

        private static void HandleCheckLocationCommand(MCommand cmd, Dialog dialog)
        {
            ArchipelagoClient ap = WolapPlugin.Archipelago;
            string locationName = cmd.StrArg(0);
            ap.SendLocationCheck(locationName);

            if (ap.IsConnected)
            {
                Traverse traverse = Traverse.Create(dialog);
                OptionsIconAndSay addItemPrefab = traverse.Field("addItemPrefab").GetValue<OptionsIconAndSay>();

                WolapPlugin.Log.LogInfo($"Scouting location {locationName}");
                var locationId = ap.Session.Locations.GetLocationIdFromName(ap.Session.ConnectionInfo.Game, locationName);
                ap.Session.Locations.ScoutLocationsAsync([locationId]).ContinueWith(locationInfoPacket =>
                {
                    foreach (ItemInfo itemInfo in locationInfoPacket.Result.Values)
                    {
                        OptionsIconAndSay itemRow = UnityEngine.Object.Instantiate<OptionsIconAndSay>(addItemPrefab);
                        itemRow.textFormat = "You found an item: <b>{0}</b>";
                        itemRow.textInsert = itemInfo.ItemDisplayName;
                        itemRow.tooltipText = $"Sent to: {itemInfo.Player.Name}";
                        itemRow.icon = SpriteLoader.SpriteForName("icon_archipelagopow");

                        traverse.Method("AddContent", [typeof(Component), typeof(OptionsContentBlock.Side)]).GetValue([itemRow, OptionsContentBlock.Side.None]);
                        traverse.Method("CompAddStuff", [typeof(Component)]).GetValue<Component>([itemRow]);
                    }
                }).Wait(TimeSpan.FromSeconds(3));
            }
        }

        private static void HandleAddNPCStoreCheckCommand(MCommand cmd)
        {
            if (cmd.argCount != 1)
            {
                cmd.LogError("only expects a check location name, but got " + cmd.argChunk);
                return;
            }

            string locationName = cmd.StrArg(0);
            ShopCheckLocation check = ArchipelagoClient.ShopCheckLocations.Find(check => check.Name == locationName);
            if (check == null)
            {
                cmd.LogError("could not find the ShopCheckLocation for location " + locationName);
                return;
            }

            MPlayer.instance.data.Add(Constants.UnlockedShopCheckFlagPrefix + check.Name.Replace(" ", ""), "1");
            WolapPlugin.Log.LogInfo($"Unlocked shop check [{check.Name}]");

            WolapPlugin.Archipelago.PopulateShopCheckItemInfo([check]);
            WolapPlugin.Archipelago.AddCheckToShop(check);
        }

        private static void HandleMissedCheckCommand(MCommand cmd)
        {
            if (cmd.argCount != 1)
            {
                cmd.LogError("only expects a check location name, but got " + cmd.argChunk);
                return;
            }

            var flags = MPlayer.instance.data;
            var locationName = cmd.StrArg(0);
            if (flags.ContainsKey(Constants.GotCheckFlagPrefix + locationName.Replace(" ", "")) || flags.ContainsKey(Constants.AddedShopCheckFlagPrefix + locationName.Replace(" ", ""))) return;

            ShopCheckLocation check = new ShopCheckLocation(locationName, "dirtwatergeneral", 1000);
            long checkID = WolapPlugin.Archipelago.Session.Locations.GetLocationIdFromName(Constants.GameName, check.Name);

            bool foundItemInfo = false;
            WolapPlugin.Archipelago.Session.Locations.ScoutLocationsAsync([checkID]).ContinueWith(locationInfoPacket =>
            {
                if (locationInfoPacket.Result == null || locationInfoPacket.Result.Values.Count == 0) return;

                ItemInfo itemInfo = locationInfoPacket.Result.Values.First();
                check.ApItemInfo = itemInfo;
                foundItemInfo = true;
            }).Wait(TimeSpan.FromSeconds(10));

            if (!foundItemInfo)
            {
                WolapPlugin.Log.LogInfo($"Tried to generate shop item for missed check [{locationName}], but could not retrieve the item info. This location may be disabled by an AP option.");
                return;
            }

            WolapPlugin.Log.LogInfo($"Retrieved item info for missed check [{check.Name}].");
            MItem newItem = WolapPlugin.Archipelago.AddCheckToShop(check);
            MItem shopItem = MPlayer.instance.stores[check.ShopID].items.Values.Where(item => item.data["description"] == newItem.data["description"]).First(); //There HAS to be a better way to do this
            shopItem.data["description"] += $"\n\nMissed check originally located at <b>{check.Name}</b>";
        }

        [HarmonyPatch(typeof(MPlayer), "NSkillLevel")]
        [HarmonyPostfix]
        static void HasPersuasionSkillPatch(MPlayer __instance, ref int __result, string strSkill)
        {
            //NSkillLevel normally just returns the base skill level -- calling GetString should return the enchantment-adjusted level, so hasskill(intimidatin/outfoxin/hornswogglin) script calls will be affected by Persuadin' level
            if (WolapPlugin.ModDataLoaded && (strSkill == "intimidatin" || strSkill == "outfoxin" || strSkill == "hornswogglin"))
            {
                if (int.TryParse(__instance.GetString(strSkill), out int adjustedSkill)) __result = adjustedSkill;
            }
        }

        //Runs when a WAA (area) is loaded
        [HarmonyPatch(typeof(Waa), "RunInitScript")]
        [HarmonyPostfix]
        static void TryReconnectToAPOnWaaLoadPatch(Waa __instance)
        {
            if (WolapPlugin.ModDataLoaded && !WolapPlugin.Archipelago.IsConnected)
            {
                WolapPlugin.Archipelago.TryReconnect();
            }
        }
    }
}
