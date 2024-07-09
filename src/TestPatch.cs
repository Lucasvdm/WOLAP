using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace WOLAP
{
    [HarmonyPatch]
    internal class TestPatch
    {
        [HarmonyPatch(typeof(Controls), "allowDevKeys", MethodType.Getter)]
        [HarmonyPostfix]
        static bool DevKeyAllowancePatch(bool allowed) => true;

        [HarmonyPatch(typeof(DebugOverlay), "Update")]
        [HarmonyPostfix]
        static void DebugOverlayOpenKeyPatch()
        {
            if (Controls.dev.GetKeycodeDown(DebugOverlay.TOGGLE_VISIBLE_KEY))
            {
                WestOfLoathing.instance.state_machine.Push(new DebugOverlayState());
            }
        }
    }
}
