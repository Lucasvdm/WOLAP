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

            if (!isUiModified && IsReadyToModifyUI())
            {
                isUiModified = true;
                SetUpUIChanges();
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

        private bool IsReadyToModifyUI()
        {
            return UI.instance != null && WolapAssets != null && WestOfLoathing.instance.state_machine.IsState(GameplayState.NAME);
        }

        //TODO: Define sizing using a HorizontalLayoutGroup or something on the parent frame instead of setting anchors in code, which would also allow for resizing the frame to fit longer/shorter item names
        private void SetUpUIChanges()
        {
            var ropeFrame = WolapAssets.LoadAsset<GameObject>(Constants.PluginAssetsPath + "/ui/popup frame.prefab");
            
            var receiptBox = Instantiate(ropeFrame);
            AttachRopeKnotScripts(receiptBox);
            receiptBox.transform.SetParent(UI.instance.GetComponentInParent<Canvas>().transform, false);
            var invs = Resources.FindObjectsOfTypeAll<Inventory>();
            var inventory = invs.First();
            var itemBox = Instantiate<InventoryItem>(inventory.itemPrefab);
            itemBox.GetComponent<Button>().onClick.RemoveAllListeners();
            MItem item = ModelManager.GetItem("boots_barnaby");
            itemBox.item = item;
            itemBox.sprite = SpriteLoader.SpriteForName(item.data["icon"]);
            itemBox.style = Inventory.Style.TwoColumn;
            itemBox.quantity = 1;
            itemBox.transform.SetParent(receiptBox.transform, false);
            var rect = itemBox.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.05f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);
            Traverse traverse = Traverse.Create(itemBox);
            WolText itemText = traverse.Field("title").GetValue<WolText>();
            FontSwitcher.AddText(itemText);

            var rcvText = new GameObject("Received Text").AddComponent<WolText>();
            rcvText.text = "Received";
            rcvText.color = Color.black;
            rcvText.resizeTextForBestFit = true;
            rcvText.alignment = TextAnchor.MiddleLeft;
            rcvText.transform.SetParent(receiptBox.transform, false);
            var rcvTextRect = rcvText.GetComponent<RectTransform>();
            rcvTextRect.anchorMin = new Vector2(0.05f, 0.05f);
            rcvTextRect.anchorMax = new Vector2(0.28f, 0.95f);
            rcvTextRect.sizeDelta = Vector2.zero;
            FontSwitcher.AddText(rcvText);
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
