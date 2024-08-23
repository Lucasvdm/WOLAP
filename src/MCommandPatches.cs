using System;
using System.Collections.Generic;
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
    }
}
