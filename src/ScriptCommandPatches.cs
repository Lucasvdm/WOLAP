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
            switch (s)
            {
                case "checklocation":
                    __result = MCommand.Op.STATESHARE; //Unused Stadia-exclusive command
                    break;
                case "addnpcstorecheck":
                    __result = MCommand.Op.SWAMPSPRITE; //Duplicate/alternate spelling of SWAPSPRITE, not used anywhere, and SWAPSPRITE is only for combat anyway
                    break;
            }
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
                case MCommand.Op.STATESHARE: //checklocation
                case MCommand.Op.SWAMPSPRITE: //addnpcstorecheck
                    //Could/should maybe use the mod logger instead, but this is consistent with other commands and should show in the log/debug window anyway
                    if (__instance.argCount == 0) __instance.LogError("needs a check ID, got no arguments.");
                    else
                    {
                        //TODO: More logic will probably be needed here later
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
            switch (cmd.op)
            {
                case MCommand.Op.STATESHARE:
                    HandleCheckLocationCommand(cmd, __instance);
                    break;
                case MCommand.Op.SWAMPSPRITE:
                    HandleAddNPCStoreCheckCommand(cmd);
                    break;
                default:
                    return;
            }

            __result = true; //Usually true by default, gets set to false by some dialog-closing commands or errors, but most Ops skip an assignment to false at the end of the method that will get caught before this patch
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
