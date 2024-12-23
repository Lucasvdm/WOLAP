using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.SceneManagement;

namespace WOLAP
{
    [HarmonyPatch]
    internal class LoadSaveDataPatches
    {
        private static bool ModDataLoaded = false;

        [HarmonyPatch(typeof(SavedGame), "NewGame")]
        [HarmonyPostfix]
        private static void NewGamePatch(string strNameFirst, string strNameLast, string strNameFull, bool fIsCowgirl, int meatReward, string strAnimal, ref string __result)
        {
            WolapPlugin.Log.LogInfo("Created new modded Archipelago save file.");
            MPlayer.instance.AddProperty(Constants.ModdedSaveProperty, "1");

            Traverse traverse = Traverse.Create(typeof(SavedGame));
            traverse = traverse.Method("SaveInternal", [typeof(string), typeof(string)]);
            __result = traverse.GetValue<string>(["create_character", null]);
        }

        //Patching handled in WolapPlugin rather than with attributes because of LoadInternal's weird signature (overloaded with a private enum parameter type)
        private static void LoadInternalPatch(string strLoadName, object lflags, ref string strWaaTeleport)
        {
            if (WestOfLoathing.instance.state_machine.IsState(GameplayState.NAME))
            {
                ProcessSaveLoaded();
            }
        }

        private static void ProcessSaveLoaded()
        {
            bool isModdedSave = MPlayer.instance.data.TryGetValue(Constants.ModdedSaveProperty, out _);
            WolapPlugin.Log.LogInfo((isModdedSave ? "Modded" : "Unmodded") + " save loaded.");

            if (!ModDataLoaded && isModdedSave) LoadWOLAPData();
            else if (ModDataLoaded && !isModdedSave)
            {
                ModelLoader.Instance.LoadLocal();
                ModDataLoaded = false;
                WolapPlugin.Log.LogInfo("Reloaded original unmodded JSON data.");
            }

            WolapPlugin.Archipelago.Connect("Lucas_WOL");
        }

        private static async void LoadWOLAPData()
        {
            byte[] fileData;
            using (var dataStream = typeof(WolapPlugin).Assembly.GetManifestResourceStream("WOLAP.src.custom_data.json"))
            {
                fileData = new byte[dataStream.Length];
                await dataStream.ReadAsync(fileData, 0, (int)dataStream.Length);
            }

            if (fileData == null || fileData.Length == 0) WolapPlugin.Log.LogError("Mod data file failed to load!");
            else
            {
                string fileText = Encoding.UTF8.GetString(fileData);
                WolapPlugin.Log.LogInfo("Parsing custom JSON data...");
                ModelManager.Instance.Parse(fileText, true);
                ModDataLoaded = true;
                WolapPlugin.Log.LogInfo("Custom JSON data parsed and merged.");
            }
        }

        [HarmonyPatch(typeof(ModelManager), "ParseScripts")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ParseScriptsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var insts = new List<CodeInstruction>(instructions);

            //Potentially brittle search conditions, but I doubt this game's code will ever be updated significantly, and compatibility with other mods is not a priority for this project
            int ifOverwriteIdx = insts.FindIndex(inst => inst.Is(OpCodes.Ldfld, m_fOverwriteOk));
            if (ifOverwriteIdx == -1)
            {
                WolapPlugin.Log.LogError("Failed to transpile ParseScripts! Could not find the m_fOverwriteOk check.");
                return instructions;
            }

            int loadTextIdx = insts.FindIndex(ifOverwriteIdx, inst => inst.opcode == OpCodes.Ldloc_1);
            if (loadTextIdx == -1)
            {
                WolapPlugin.Log.LogError("Failed to transpile ParseScripts! Could not find the ldloc.1 load.");
                return instructions;
            }

            insts.RemoveAt(loadTextIdx); //Remove load for local variable "text", don't need it
            int assignmentIdx = insts.FindIndex(loadTextIdx, inst => inst.opcode == OpCodes.Callvirt);
            if (assignmentIdx == -1)
            {
                WolapPlugin.Log.LogError("Failed to transpile ParseScripts! Could not find the set_Item callvirt.");
                return instructions;
            }

            insts.RemoveAt(assignmentIdx); //Remove call to set_Item assignment (this.scripts[text] = mscript)

            //Insert instruction to call InjectedHelpers.OverwriteScriptStates(this.scripts, mscript);
            insts.Insert(assignmentIdx, new CodeInstruction(OpCodes.Call, m_OverwriteScriptStates));

            return insts;
        }

        static MethodInfo m_OverwriteScriptStates = SymbolExtensions.GetMethodInfo(() => InjectedHelpers.OverwriteScriptStates(null, null));
        static FieldInfo m_fOverwriteOk = typeof(ModelManager).GetField("m_fOverwriteOk", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
