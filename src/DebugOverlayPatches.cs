using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

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

                if (gsm.HasState(debugState))
                {
                    WolapPlugin.Log.LogInfo("Closing debug overlay.");
                    gsm.Pop(debugState);
                }
                else
                {
                    WolapPlugin.Log.LogInfo("Opening debug overlay.");
                    gsm.Push(new DebugOverlayState());
                }

                gsm.LogStates();
            }
        }

        //Add listener so command input is only submitted when Enter is pressed, not when focus is lost
        [HarmonyPatch(typeof(DebugOverlay), "Awake")]
        [HarmonyPostfix]
        static void SubmitInputControlPatch(DebugOverlay __instance)
        {
            __instance.commandLine.onEndEdit.AddListener(delegate { SubmitCommandOnEnter(__instance); });
        }

        static void SubmitCommandOnEnter(DebugOverlay __instance)
        {
            InputField commandLine = __instance.commandLine;
            if (commandLine != null && commandLine.text.Length > 0 && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                WolapPlugin.Log.LogInfo($"Running debug command '{commandLine.text}'");
                __instance.RunString(commandLine.text);
                __instance.commandLine.text = "";
            }
        }

        [HarmonyPatch(typeof(DebugOverlayState), "OnUpdate")]
        [HarmonyPrefix]
        static bool DebugOverlayStateUpdatePatch()
        {
            return false;
        }

        //TODO: Move to another class
        [HarmonyPatch(typeof(GameStateMachine), "LogStates")]
        [HarmonyPrefix]
        static void LogStatesPatch(GameStateMachine __instance)
        {
            WolapPlugin.Log.LogInfo("-- Current game states --");

            List<GameState> states = Traverse.Create(__instance).Field<List<GameState>>("m_aryState").Value;

            if (states.Count == 0) return;

            for (int i = 0; i < states.Count; i++)
            {
                WolapPlugin.Log.LogInfo($"State {i}: {states[i].name}");
            }

            return;
        }
    }
}
