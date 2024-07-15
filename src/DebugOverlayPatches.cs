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
            traverse.Method("UpdateBugNotification").GetValue();

            //traverse.Method("UpdateFpsInfo").GetValue(); // F
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

                //gsm.LogStates();
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

        [HarmonyPatch(typeof(DebugOverlay), "RunSlashCommand")]
        [HarmonyPrefix]
        static bool SlashCommandOverridePatch(string strCommandOrig)
        {
            string[] args = strCommandOrig.Split(new char[]{' '});
            switch (args[0])
            {
                case "tp":
                case "teleport":
                    TryTeleportCommand(args);
                    break;
                default:
                    WolapPlugin.Log.LogWarning($"'/{strCommandOrig}' is not a recognized command.");
                    break;
            }
            return false;
        }

        //TODO: Add help info, printed out on "/tp help" or when no arguments provided. Similar for other commands. Just another method for each or some centralized help text location?
        static void TryTeleportCommand(string[] args)
        {
            if (args.Length < 2)
            {
                WolapPlugin.Log.LogWarning("Teleport command failed: No target provided.");
                return;
            }

            string target = String.Join(" ", args.Skip(1));

            MWaa mwaa;
            if (!ModelManager.Instance.waas.TryGetValue(target, out mwaa)) //Search by ID
            {
                foreach (MWaa waa in ModelManager.Instance.waas.Values) //Try searching by readable name
                {
                    if (waa.name == target)
                    {
                        mwaa = waa;
                        break;
                    }
                }

                if (mwaa == null)
                {
                    WolapPlugin.Log.LogWarning($"Teleport command failed: Target '{target}' could not be found.");
                    return;
                }
            }

            WolapPlugin.Log.LogInfo($"Teleporting to {target}...");
            Waa.TeleportToWaa(mwaa.id);
        }

        //Overrides this OnUpdate and instead handles closing in the toggle (DebugOverlay Update) so the opening and closing don't conflict
        [HarmonyPatch(typeof(DebugOverlayState), "OnUpdate")]
        [HarmonyPrefix]
        static bool DebugOverlayStateOnUpdateOverridePatch()
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
