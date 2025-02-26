using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Archipelago.MultiClient.Net.Models;
using HarmonyLib;
using UnityEngine;

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
    }
}
