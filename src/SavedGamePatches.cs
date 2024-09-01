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

        //Patching handled in WolapPlugin rather than with attributes because of LoadInternal's weird signature (overloaded with a private enum parameter type)
        private static void LoadInternalPatch(string strLoadName, object lflags, ref string strWaaTeleport)
        {
            //TODO: Check if save is modded, via a flag set on save file creation

            if (!ModDataLoaded && MPlayer.instance.data.Keys.Count > 0) LoadWOLAPData();
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
