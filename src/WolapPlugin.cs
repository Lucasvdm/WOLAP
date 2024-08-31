using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace WOLAP
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class WolapPlugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "lucasvdm.westofloathing.aprandomizer";
        internal const string PluginName = "West of Loathing Archipelago Randomizer";
        internal const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static AssetBundle WolapAssets;

        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            Log.LogInfo($"Plugin {PluginGuid} is loaded!");
            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            LoadAssets();
        }

        private void LoadAssets()
        {
            var assembly = typeof(WolapPlugin).Assembly;
            var assetsStream = assembly.GetManifestResourceStream("WOLAP.assets.wolap_assets");
            WolapAssets = AssetBundle.LoadFromStream(assetsStream); //Should probably do asynchronously, but using this for testing for now as the bundle is small

            if (WolapAssets == null) Log.LogError("Failed to load WOLAP asset bundle!");
            else Log.LogInfo("WOLAP asset bundle loaded!");
        }
    }
}
