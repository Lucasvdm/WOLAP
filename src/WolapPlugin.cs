using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WOLAP
{
    [BepInPlugin(Constants.PluginGuid, Constants.PluginName, Constants.PluginVersion)]
    public class WolapPlugin : BaseUnityPlugin
    {
        internal static Harmony Harmony;
        internal static ManualLogSource Log;
        internal static AssetBundle Assets;
        internal static ArchipelagoClient Archipelago;
        internal static UIManager UIManager;

        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            Log.LogInfo($"Plugin {Constants.PluginGuid} is loaded!");
            Harmony = new Harmony(Constants.PluginGuid);
            Harmony.PatchAll(Assembly.GetExecutingAssembly());

            ProcessManualPatches();

            StartCoroutine(LoadAssets());

            var uiManagerGO = new GameObject("WOLAP UI Manager");
            DontDestroyOnLoad(uiManagerGO);
            UIManager = uiManagerGO.AddComponent<UIManager>();

            Archipelago = new ArchipelagoClient();
        }

        private void Update()
        {
            Archipelago.Update();

            //if (Controls.actual.GetKeycodeDown(KeyCode.RightBracket)) Log.LogInfo("Current game state: " + WestOfLoathing.instance.state_machine.state.name);
        }

        private static void ProcessManualPatches()
        {
            //Have to do this patch manually with reflection because it's an overloaded method with one of its parameters being a private enum type
            Type loadFlagsType = typeof(SavedGame).GetNestedType("LoadFlags", BindingFlags.NonPublic);
            var methodParams = new[] { typeof(string), loadFlagsType, typeof(string).MakeByRefType() };
            var savedGameLoadInternal = typeof(SavedGame).GetMethod("LoadInternal", BindingFlags.NonPublic | BindingFlags.Static, Type.DefaultBinder, methodParams, null);
            var loadInternalPostfix = typeof(LoadSaveDataPatches).GetMethod("LoadInternalPatch", BindingFlags.NonPublic | BindingFlags.Static);
            Harmony.Patch(savedGameLoadInternal, postfix: new HarmonyMethod(loadInternalPostfix));
        }

        private static IEnumerator LoadAssets()
        {
            yield return new WaitUntil(() => WestOfLoathing.instance != null && WestOfLoathing.instance.state_machine.IsState(TitleStateWaa.NAME));

            var assembly = typeof(WolapPlugin).Assembly;
            var assetsStream = assembly.GetManifestResourceStream("WOLAP.assets.wolap_assets");
            var bundleLoadRequest = AssetBundle.LoadFromStreamAsync(assetsStream);
            yield return new WaitUntil(() => bundleLoadRequest.isDone);

            Assets = bundleLoadRequest.assetBundle;
            if (Assets == null)
            {
                Log.LogError("Failed to load WOLAP asset bundle!");
                yield break;
            }
            else Log.LogInfo("WOLAP asset bundle loaded!");

            //AssetBundleInfo struct is private, need to use reflection to initialize and set values (and add it to the dictionary)
            Type assetBundleInfoType = typeof(AssetBundleManager).GetNestedType("AssetBundleInfo", BindingFlags.NonPublic);
            var assetBundleInfo = Activator.CreateInstance(assetBundleInfoType); //Have to use this instead of Harmony's Type.CreateInstance ("assetBundleInfoType.CreateInstance"), that creates errors on current version
            assetBundleInfoType.GetField("pathPrefix").SetValue(assetBundleInfo, Constants.PluginAssetsPath);
            assetBundleInfoType.GetField("isScene").SetValue(assetBundleInfo, false);
            assetBundleInfoType.GetField("assbun").SetValue(assetBundleInfo, Assets);

            Traverse traverse = Traverse.Create(AssetBundleManager.instance);
            var assetBundleInfoDict = traverse.Field("m_mpStrAbi").GetValue();
            var dictItemProp = assetBundleInfoDict.GetType().GetProperty("Item");
            dictItemProp.SetValue(assetBundleInfoDict, assetBundleInfo, [Constants.PluginNameShort]);
        }
    }
}
