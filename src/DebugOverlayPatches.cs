using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace WOLAP
{
    [HarmonyPatch]
    internal class DebugOverlayPatches
    {
        [HarmonyPatch(typeof(Controls), "allowDevKeys", MethodType.Getter)]
        [HarmonyPostfix]
        static bool DevKeyAllowancePatch(bool allowed) => true;

        [HarmonyPatch(typeof(DebugOverlay), "Update")]
        [HarmonyPostfix]
        static void DebugOverlayUpdatePatch(DebugOverlay __instance)
        {
            UpdateOverlayToggle();
            Traverse traverse = Traverse.Create(__instance);
            traverse.Method("UpdateClutterInfo").GetValue(); //Shift + C
            traverse.Method("UpdateDebugInfo").GetValue(); // Ctrl + Shift + I
            traverse.Method("UpdateFpsInfo").GetValue(); // F
            traverse.Method("UpdateBugNotification").GetValue();
            //traverse.Method("UpdateDomeLights").GetValue(); // Ctrl + L -- Not implemented
        }

        static void UpdateOverlayToggle()
        {
            if (Controls.dev.GetKeycodeDown(DebugOverlay.TOGGLE_VISIBLE_KEY))
            {
                GameStateMachine gsm = WestOfLoathing.instance.state_machine;
                string debugState = DebugOverlayState.NAME;

                if (gsm.IsState(debugState))
                {
                    WolapPlugin.Log.LogInfo("Closing debug overlay.");
                    gsm.Pop(debugState);
                }
                else
                {
                    WolapPlugin.Log.LogInfo("Opening debug overlay.");
                    gsm.Push(new DebugOverlayState());
                }
            }
        }
    }
}
