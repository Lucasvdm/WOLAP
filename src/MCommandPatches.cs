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
            if (s == "checklocation") __result = MCommand.Op.STATESHARE;
        }

        [HarmonyPatch(typeof(MCommand), "Execute")]
        [HarmonyPrefix]
        static void ExecutePatch(MCommand __instance, Action<MCommand> callback)
        {
            if (__instance.op == MCommand.Op.STATESHARE) //checklocation
            {
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
            }
        }
    }
}
