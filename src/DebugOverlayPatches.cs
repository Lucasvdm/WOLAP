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
        const int MaxOutputLines = 50;

        internal enum LogLevel
        {
            Info,
            Warning,
            Error,
            Command
        }

        static Dictionary<LogLevel, string> logColors = new Dictionary<LogLevel, string>()
        {
            [LogLevel.Command] = "grey",
            [LogLevel.Info] = "black",
            [LogLevel.Warning] = "orange",
            [LogLevel.Error] = "red"
        };

        static Text console;
        static InputField commandLine;
        public static bool IsOverlayOpen { get; private set; }
        static List<string> commandHistory;
        static int commandHistoryIndex = -1;

        [HarmonyPatch(typeof(Controls), "allowDevKeys", MethodType.Getter)]
        [HarmonyPostfix]
        static bool DevKeyAllowancePatch(bool allowed) => true;

        [HarmonyPatch(typeof(DebugOverlay), "Awake")]
        [HarmonyPostfix]
        static void DebugOverlayAwakePatch(DebugOverlay __instance)
        {
            console = __instance.consoleGroup.GetComponentInChildren<Text>();
            console.supportRichText = true;
            commandLine = __instance.commandLine;
            commandHistory = new List<string>();
        }

        [HarmonyPatch(typeof(DebugOverlay), "Update")]
        [HarmonyPostfix]
        static void DebugOverlayUpdatePatch(DebugOverlay __instance)
        {
            UpdateOverlayToggle();
            ProcessCommandLineControls(__instance);
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
                    IsOverlayOpen = false;
                }
                else
                {
                    WolapPlugin.Log.LogInfo("Opening debug overlay.");
                    gsm.Push(new DebugOverlayState());
                    IsOverlayOpen = true;
                }

                //gsm.LogStates();
            }
        }

        static void ProcessCommandLineControls(DebugOverlay __instance)
        {
            if (IsOverlayOpen)
            {
                ProcessCommandHistoryInput();
            }
        }

        static void ProcessCommandHistoryInput()
        {
            if (commandHistory.Count != 0 && commandLine.isFocused)
            {
                if (Controls.dev.GetKeycodeDown(KeyCode.UpArrow) && commandHistoryIndex > 0)
                {
                    commandHistoryIndex--;
                    commandLine.text = commandHistory.ElementAt(commandHistoryIndex);
                }
                else if (Controls.dev.GetKeycodeDown(KeyCode.DownArrow) && commandHistoryIndex < (commandHistory.Count - 1))
                {
                    commandHistoryIndex++;
                    commandLine.text = commandHistory.ElementAt(commandHistoryIndex);
                }
            }
        }

        //Add listener so command input is only submitted when Enter is pressed, not when focus is lost
        [HarmonyPatch(typeof(DebugOverlay), "Awake")]
        [HarmonyPostfix]
        static void SubmitInputControlPatch(DebugOverlay __instance)
        {
            commandLine.onEndEdit.AddListener(delegate { SubmitCommandOnEnter(__instance); });
        }

        static void SubmitCommandOnEnter(DebugOverlay __instance)
        {
            if (commandLine.text.Length > 0 && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                WolapPlugin.Log.LogInfo($"Running debug command '{commandLine.text}'");
                console.text += $"<b><color={logColors[LogLevel.Command]}>> {commandLine.text}</color></b>\n";
                __instance.RunString(commandLine.text);
                commandHistory.Add(commandLine.text);
                commandHistoryIndex = commandHistory.Count;
                commandLine.text = "";
                commandLine.ActivateInputField(); //Keep focus after entering a command
            }
        }

        //JSON script commands and more can be run without a slash, refer to MCommand.Op in the WOL code -- TODO: Have a "help" or "commands" command to list all of these
        [HarmonyPatch(typeof(DebugOverlay), "RunSlashCommand")]
        [HarmonyPrefix]
        static bool SlashCommandOverridePatch(DebugOverlay __instance, string strCommandOrig)
        {
            string[] args = strCommandOrig.Split(new char[]{' '});
            switch (args[0])
            {
                case "flags":
                    PrintStandardFlags();
                    break;
                case "dailyflags":
                    PrintDailyFlags();
                    break;
                case "permaflags":
                    PrintPermaFlags();
                    break;
                case "flag":
                    PrintStandardFlag(args);
                    break;
                case "dailyflag":
                    PrintDailyFlag(args);
                    break;
                case "permaflag":
                    PrintPermaFlag(args);
                    break;
                case "setflag":
                    SetStandardFlag(args);
                    break;
                case "setdailyflag":
                    SetDailyFlag(args);
                    break;
                case "setpermaflag":
                    SetPermaFlag(args);
                    break;
                case "properties":
                    PrintProperties();
                    break;
                case "removeproperty":
                    RemoveProperty(args);
                    break;
                case "tp":
                case "teleport":
                    TryTeleportCommand(args);
                    break;
                case "cls": //"Legacy" command, included for consistency because it's listed in the debug console reference bucket in data.json
                case "clr":
                case "clear":
                    ClearConsole();
                    break;
                default:
                    if (MCommand.StringToOp(args[0]) != MCommand.Op.INVALID)
                    {
                        __instance.RunString(strCommandOrig); //Re-runs the same command without the slash
                    }
                    else
                    {
                        LogDebugConsoleMessage($"'{strCommandOrig}' is not a recognized command.", LogLevel.Warning);
                    }
                    break;
            }
            return false;
        }

        static void PrintStandardFlags()
        {
            PrintFlags("Standard", MPlayer.instance.data);
        }

        static void PrintDailyFlags()
        {
            PrintFlags("Daily", MPlayer.instance.dailyflags.data);
        }

        static void PrintPermaFlags()
        {
            PrintFlags("Perma", MPlayer.instance.permaflags.data);
        }

        static void PrintFlags(string flagType, Dictionary<string, string> flags)
        {
            LogDebugConsoleMessage($"-- {flagType} Flags --", LogLevel.Info);
            int count = 0;
            foreach (KeyValuePair<string, string> flag in flags)
            {
                if (count == MaxOutputLines)
                {
                    LogDebugConsoleMessage("Output too long for this basic debug console, for full output see the terminal window...", LogLevel.Warning);
                }

                string outputLine = $"{flag.Key}: {flag.Value}";
                if (count < MaxOutputLines)
                {
                    LogDebugConsoleMessage(outputLine, LogLevel.Info);
                }
                else
                {
                    WolapPlugin.Log.LogInfo(outputLine);
                }
                
                count++;
            }
        }

        static void PrintStandardFlag(string[] args)
        {
            PrintFlag(args, MPlayer.instance.data);
        }

        static void PrintDailyFlag(string[] args)
        {
            PrintFlag(args, MPlayer.instance.dailyflags.data);
        }

        static void PrintPermaFlag(string[] args)
        {
            PrintFlag(args, MPlayer.instance.permaflags.data);
        }

        static void PrintFlag(string[] args, Dictionary<string, string> flags)
        {
            if (args.Length < 2)
            {
                LogDebugConsoleMessage("Print flag command failed: No flag provided", LogLevel.Error);
                return;
            }

            string flag = String.Join(" ", args.Skip(1)); //Should be unnecessary for flags, they should all be one 'word' -- but useful if only for the failure log

            if (MPlayer.instance.data.TryGetValue(flag, out string value))
            {
                LogDebugConsoleMessage($"{flag}: {value}", LogLevel.Info);
            }
            else
            {
                LogDebugConsoleMessage($"Print flag command failed: Could not find flag '{flag}'", LogLevel.Warning);
            }
        }

        static void SetStandardFlag(string[] args)
        {
            SetFlag(args, MPlayer.instance.data);
        }

        static void SetDailyFlag(string[] args)
        {
            SetFlag(args, MPlayer.instance.dailyflags.data);
        }

        static void SetPermaFlag(string[] args)
        {
            SetFlag(args, MPlayer.instance.permaflags.data);
        }

        static void SetFlag(string[] args, Dictionary<string, string> flags)
        {
            if (args.Length < 3 || args.Length > 3)
            {
                LogDebugConsoleMessage("Set flag command failed: Should be in the form '/<setcommand> <flag> <value>'", LogLevel.Error);
                return;
            }

            string flag = args[1];
            string value = args[2];

            if (flags.ContainsKey(flag)) flags[flag] = value;
            else flags.Add(flag, value);

            LogDebugConsoleMessage($"Set flag '{flag}' to value: '{value}'", LogLevel.Info);
        }

        static void PrintProperties()
        {
            LogDebugConsoleMessage($"-- Properties --", LogLevel.Info);
            int count = 0;
            foreach (string property in MPlayer.instance.propertyNames)
            {
                if (count == MaxOutputLines)
                {
                    LogDebugConsoleMessage("Output too long for this basic debug console, for full output see the terminal window...", LogLevel.Warning);
                }

                if (MPlayer.instance.data.TryGetValue(property, out string value))
                {
                    string outputLine = $"{property}: {value}";
                    if (count < MaxOutputLines) LogDebugConsoleMessage(outputLine, LogLevel.Info);
                    else WolapPlugin.Log.LogInfo(outputLine);
                }
                else
                {
                    string outputLine = $"Could not find a value for property '{property}'";
                    if (count < MaxOutputLines) LogDebugConsoleMessage(outputLine, LogLevel.Warning);
                    else WolapPlugin.Log.LogWarning(outputLine);
                }

                count++;
            }
        }

        static void RemoveProperty(string[] args)
        {
            if (args.Length < 2)
            {
                LogDebugConsoleMessage("Remove property command failed: No property provided", LogLevel.Error);
                return;
            }

            string property = String.Join(" ", args.Skip(1));
            MPlayer mPlayer = MPlayer.instance;
            if (!mPlayer.propertyNames.Contains(property))
            {
                LogDebugConsoleMessage($"Could not find property '{property}' to remove", LogLevel.Warning);
            }
            else
            {
                mPlayer.data.Remove(property);
                mPlayer.propertyNames.Remove(property);
                LogDebugConsoleMessage($"Removed property '{property}'", LogLevel.Info);
            }
        }

        //TODO: Add help info, printed out on "/tp help" or when no arguments provided. Similar for other commands. Just another method for each or some centralized help text location?
        static void TryTeleportCommand(string[] args)
        {
            if (args.Length < 2)
            {
                LogDebugConsoleMessage("Teleport command failed: No target provided", LogLevel.Error);
                return;
            }

            string target = String.Join(" ", args.Skip(1));

            MWaa mwaa;
            if (!ModelManager.Instance.waas.TryGetValue(target, out mwaa)) //Search by ID
            {
                foreach (MWaa waa in ModelManager.Instance.waas.Values) //Try searching by readable name -- TODO: This name isn't unique, so account for cases where there are multiple and log an error or something
                {
                    if (waa.name == target)
                    {
                        mwaa = waa;
                        break;
                    }
                }

                if (mwaa == null)
                {
                    LogDebugConsoleMessage($"Teleport command failed: Target '{target}' could not be found", LogLevel.Error);
                    return;
                }
            }

            LogDebugConsoleMessage($"Teleporting to {target}...", LogLevel.Info);
            Waa.TeleportToWaa(mwaa.id);
        }

        static void ClearConsole()
        {
            console.text = "";
            WolapPlugin.Log.LogInfo("Cleared debug console");
        }

        [HarmonyPatch(typeof(MEvalContext), "LogErrorStatic")]
        [HarmonyPostfix]
        static void LogCommandErrorStaticPatch(string strMessage)
        {
            string richTextFormattedMessage = strMessage.Replace('<', '(').Replace('>', ')');

            LogDebugConsoleMessage(richTextFormattedMessage, LogLevel.Error);
        }

        [HarmonyPatch(typeof(MEvalContext), "LogError")]
        [HarmonyPostfix]
        static void LogCommandErrorPatch(string strMessage)
        {
            string richTextFormattedMessage = strMessage.Replace('<', '(').Replace('>', ')');

            LogDebugConsoleMessage(richTextFormattedMessage, LogLevel.Error);
        }

        static void LogDebugConsoleMessage(string message, LogLevel logLevel)
        {
            int characterLimit = 3000; //Approximate character limit due to Unity UI Text vertex limit

            string richText = $"<color={logColors[logLevel]}>{message}</color>\n";

            if ((console.text + richText).Length > characterLimit) 
            {
                string[] lines = console.text.Trim().Split(new char[]{'\n'});
                string currentCommand = lines[lines.Length - 1];
                console.text = "";
                LogDebugConsoleMessage($"Console cleared automatically due to {characterLimit} character limit...", LogLevel.Warning);
                console.text += currentCommand + "\n";
            }

            console.text += richText;

            switch (logLevel)
            {
                case LogLevel.Info:
                    WolapPlugin.Log.LogInfo(message);
                    break;
                case LogLevel.Warning:
                    WolapPlugin.Log.LogWarning(message);
                    break;
                case LogLevel.Error:
                    WolapPlugin.Log.LogError(message);
                    break;
            }
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
