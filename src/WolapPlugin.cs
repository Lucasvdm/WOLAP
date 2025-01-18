using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.IO;
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
        internal static AssetBundle WolapAssets;
        internal static ArchipelagoClient Archipelago;

        private bool isUiModified;

        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            Log.LogInfo($"Plugin {Constants.PluginGuid} is loaded!");
            Harmony = new Harmony(Constants.PluginGuid);
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

            if (!isUiModified && UI.instance != null && WolapAssets != null)
            {
                SetUpUIChanges();
                isUiModified = true;
            }

            Archipelago.Update();

            //if (Controls.actual.GetKeycodeDown(KeyCode.Quote)) Log.LogInfo("Current game state: " + WestOfLoathing.instance.state_machine.state.name);
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
            assetBundleInfoType.GetField("pathPrefix").SetValue(assetBundleInfo, Constants.PluginAssetsPath);
            assetBundleInfoType.GetField("isScene").SetValue(assetBundleInfo, false);
            assetBundleInfoType.GetField("assbun").SetValue(assetBundleInfo, WolapAssets);

            Traverse traverse = Traverse.Create(AssetBundleManager.instance);
            var assetBundleInfoDict = traverse.Field("m_mpStrAbi").GetValue();
            var dictItemProp = assetBundleInfoDict.GetType().GetProperty("Item");
            dictItemProp.SetValue(assetBundleInfoDict, assetBundleInfo, [Constants.PluginNameShort]);
        }

        private void SetUpUIChanges()
        {
            var ropeFrame = WolapAssets.LoadAsset<GameObject>(Constants.PluginAssetsPath + "/ui/popup frame.prefab");
            
            var receiptBox = Instantiate(ropeFrame);
            AttachRopeKnotScripts(receiptBox);
            receiptBox.transform.SetParent(UI.instance.GetComponentInParent<Canvas>().transform, false);
            var testText = new GameObject("Receipt Text");
            var textComp = testText.AddComponent<Text>();
            textComp.text = "Test";
            textComp.color = Color.red;
            textComp.fontSize = 36;
            textComp.font = Font.GetDefault();
            testText.transform.SetParent(receiptBox.transform, false);
        }

        private void AttachRopeKnotScripts(GameObject ropeFrame)
        {
            var knotParent = ropeFrame.transform.Find(Constants.GameObjectKnotsPath);
            if (knotParent == null)
            {
                Log.LogWarning("Could not find the rope knots on " + ropeFrame.name + " to attach the RopeKnot scripts.");
                return;
            }

            foreach (Transform knot in knotParent)
            {
                knot.gameObject.AddComponent<RopeKnot>();
            }
        }
    }
}
