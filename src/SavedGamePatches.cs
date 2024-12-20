using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HarmonyLib;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace WOLAP
{
    [HarmonyPatch(typeof(SavedGame))]
    internal class SavedGamePatches
    {
        private static bool ModDataLoaded = false;

        [HarmonyPatch("NewGame")]
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
    }
}
