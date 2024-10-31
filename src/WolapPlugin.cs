using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WOLAP
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class WolapPlugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "lucasvdm.westofloathing.aprandomizer";
        internal const string PluginName = "West of Loathing Archipelago Randomizer";
        internal const string PluginNameShort = "WOLAP";
        internal const string PluginVersion = "0.1.0";
        internal const string PluginAssetsPath = "assets/wolap_assets";

        internal static Harmony Harmony;
        internal static ManualLogSource Log;
        internal static AssetBundle WolapAssets;
        internal static ArchipelagoClient Archipelago;

        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            Log.LogInfo($"Plugin {PluginGuid} is loaded!");
            Harmony = new Harmony(PluginGuid);
            Harmony.PatchAll(Assembly.GetExecutingAssembly());

            ProcessManualPatches();

            StartCoroutine(LoadAssets());

            Archipelago = new ArchipelagoClient("archipelago.gg", 55753);
        }

        private void Update()
        {
            if (Archipelago.IsConnected && WestOfLoathing.instance.state_machine.IsState(TitleStateWaa.NAME))
            {
                Archipelago.Disconnect();
            }

            Archipelago.Update();
        }

        private static void ProcessManualPatches()
        {
            //Have to do this patch manually with reflection because it's an overloaded method with one of its parameters being a private enum type
            Type loadFlagsType = typeof(SavedGame).GetNestedType("LoadFlags", BindingFlags.NonPublic);
            var methodParams = new[] { typeof(string), loadFlagsType, typeof(string).MakeByRefType() };
            var savedGameLoadInternal = typeof(SavedGame).GetMethod("LoadInternal", BindingFlags.NonPublic | BindingFlags.Static, Type.DefaultBinder, methodParams, null);
            var loadInternalPostfix = typeof(SavedGamePatches).GetMethod("LoadInternalPatch", BindingFlags.NonPublic | BindingFlags.Static);
            Harmony.Patch(savedGameLoadInternal, postfix: new HarmonyMethod(loadInternalPostfix));
        }

        private static IEnumerator LoadAssets()
        {
            var assembly = typeof(WolapPlugin).Assembly;
            var assetsStream = assembly.GetManifestResourceStream("WOLAP.assets.wolap_assets");
            var bundleLoadRequest = AssetBundle.LoadFromStreamAsync(assetsStream);
            yield return new WaitUntil(() => bundleLoadRequest.isDone);

            WolapAssets = bundleLoadRequest.assetBundle;
            if (WolapAssets == null)
            {
                Log.LogError("Failed to load WOLAP asset bundle!");
                yield break;
            }
            else Log.LogInfo("WOLAP asset bundle loaded!");

            //AssetBundleInfo struct is private, need to use reflection to initialize and set values (and add it to the dictionary)
            Type assetBundleInfoType = typeof(AssetBundleManager).GetNestedType("AssetBundleInfo", BindingFlags.NonPublic);
            var assetBundleInfo = Activator.CreateInstance(assetBundleInfoType); //Have to use this instead of Harmony's Type.CreateInstance ("assetBundleInfoType.CreateInstance"), that creates errors on current version
            assetBundleInfoType.GetField("pathPrefix").SetValue(assetBundleInfo, PluginAssetsPath);
            assetBundleInfoType.GetField("isScene").SetValue(assetBundleInfo, false);
            assetBundleInfoType.GetField("assbun").SetValue(assetBundleInfo, WolapAssets);

            Traverse traverse = Traverse.Create(AssetBundleManager.instance);
            var assetBundleInfoDict = traverse.Field("m_mpStrAbi").GetValue();
            var dictItemProp = assetBundleInfoDict.GetType().GetProperty("Item");
            dictItemProp.SetValue(assetBundleInfoDict, assetBundleInfo, [PluginNameShort]);
        }
    }
}
