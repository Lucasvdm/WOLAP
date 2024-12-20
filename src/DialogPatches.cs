using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace WOLAP
{
    [HarmonyPatch(typeof(Dialog))]
    internal class DialogPatches
    {
        private static KeyCode TOGGLE_DIALOG_DEBUG = KeyCode.Backslash;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static void DialogUpdatePatch(Dialog __instance)
        {
            if (Controls.dev.GetKeycodeDown(TOGGLE_DIALOG_DEBUG))
            {
                Traverse traverse = Traverse.Create(__instance);
                GameObject rootObject = traverse.Field("debugObjectsRoot").GetValue<GameObject>();

                WolapPlugin.Log.LogInfo($"Attempting to toggle dialog debug features! Root Object isNull: {rootObject == null}");

                if (rootObject != null)
                {
                    bool active = !rootObject.activeSelf;
                    SetDialogDebugInfoVisible(rootObject, active);
                    WolapPlugin.Log.LogInfo($"Toggling dialog debug features! Debug active: {active}");
                }
            }
        }

        [HarmonyPatch("OnCommand")]
        [HarmonyPostfix]
        static void OnCommandPatch(Dialog __instance, MEvalContext ectx, MCommand cmd, ref bool __result)
        {
            switch (cmd.op)
            {
                case MCommand.Op.STATESHARE:
                    HandleCheckLocationCommand(cmd);
                    break;
                case MCommand.Op.SWAMPSPRITE:
                    HandleAddNPCStoreCheckCommand(cmd);
                    break;
                default:
                    return;
            }

            __result = true; //Usually true by default, gets set to false by some dialog-closing commands or errors, but most Ops skip an assignment to false at the end of the method that will get caught before this patch
        }

        private static void HandleCheckLocationCommand(MCommand cmd)
        {
            string locationName = cmd.StrArg(0);
            WolapPlugin.Archipelago.SendLocationCheck(locationName);
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

        [HarmonyPatch("SetDebugObjectsVisible")]
        [HarmonyPrefix]
        static bool SetDialogDebugObjectsVisiblePatch(Dialog __instance, bool fVisible)
        {
            Traverse traverse = Traverse.Create(__instance);
            GameObject rootObject = traverse.Field("debugObjectsRoot").GetValue<GameObject>();

            //ONLY do the SetActive call if the fVisible parameter is false -- if SetDebugObjectsVisible is called with 'true' (in Update), ignore it.
            if (rootObject != null && !fVisible) rootObject.SetActive(false); 

            return false;
        }

        //Handling the real toggle in this method instead of SetDialogDebugObjectsVisible because there's an annoying call to that method in Update which always sets it to true
        static void SetDialogDebugInfoVisible(GameObject debugInfoRoot, bool visible)
        {
            debugInfoRoot.transform.position = debugInfoRoot.transform.position.WithY(0f);
            debugInfoRoot.SetActive(visible);
        }
    }
}
