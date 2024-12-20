using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;

namespace WOLAP
{
    [HarmonyPatch]
    internal class MCommandPatches
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
        static void ExecutePatch(MCommand __instance, Action<MCommand> callback)
        {
            switch(__instance.op)
            {
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
        }
    }
}
