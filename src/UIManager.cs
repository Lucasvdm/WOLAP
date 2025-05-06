using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WOLAP
{
    [HarmonyPatch]
    public class UIManager : MonoBehaviour
    {
        public static bool ConnectionInProgress;

        public ConcurrentQueue<ReceivedID> RcvItemQueue { get; private set; }

        private bool isUiModified;

        private InventoryItem rcvItemCard;
        private CanvasGroup rcvItemWindowCG;
        private CanvasGroup rcvItemItemCG;
        private float rcvItemWindowFadeTime = 0.75f;
        private float rcvItemItemFadeTime = 0.4f;
        private float rcvItemShowTime = 2.5f;
        private bool rcvItemFading;
        private bool rcvQueueInProgress;
        private Coroutine rcvQueueCoroutine;

        private static CanvasGroup apConnectionBox;
        private static WolInputField[] apConnectionInputs = new WolInputField[4];
        private static Button apConnectionButton;
        private static WolText apConnectionStatusText;

        private void Awake()
        {
            RcvItemQueue = new ConcurrentQueue<ReceivedID>();
        }

        private void Update()
        {
            if (!isUiModified && IsReadyToModifyUI())
            {
                isUiModified = true;
                SetUpUIChanges();
            }

            if (isUiModified)
            {
                //TODO: While this is in progress, hide it when opening the inventory/pause menu/whatever? It currently freezes and correctly resumes when returning to gameplay, those other states must pause the game clock or something.
                if (RcvItemQueue.Count > 0 && !rcvQueueInProgress) rcvQueueCoroutine = StartCoroutine(DisplayReceivedItemQueue());

                //Slightly more pleasant behaviour handling this here rather than in OnPush/OnPop patches for TitleStateWAA
                if (apConnectionBox.isActiveAndEnabled && !WestOfLoathing.instance.state_machine.IsState(TitleStateWaa.NAME)) apConnectionBox.gameObject.SetActive(false);
                else if (!apConnectionBox.isActiveAndEnabled && WestOfLoathing.instance.state_machine.IsState(TitleStateWaa.NAME))
                {
                    if (!WolapPlugin.Archipelago.IsConnected) EnableEditingAPSettings();
                    apConnectionBox.gameObject.SetActive(true);
                }
            }
        }

        private bool IsReadyToModifyUI()
        {
            return UI.instance != null && WolapPlugin.Assets != null;
        }
        
        private void SetUpUIChanges()
        {
            WolapPlugin.Log.LogInfo("Setting up new UI elements.");

            CreateRcvItemWindow();
            CreateAPConnectionBox();

            WolapPlugin.Log.LogInfo("Finished setting up new UI elements.");
        }

        //TODO: Define sizing using a HorizontalLayoutGroup or something on the parent frame instead of setting anchors in code, which would also allow for resizing the frame to fit longer/shorter item names
        private void CreateRcvItemWindow()
        {
            var ropeFrame = WolapPlugin.Assets.LoadAsset<GameObject>(Constants.PluginAssetsPath + "/ui/popup frame.prefab");

            var receiptBox = Instantiate(ropeFrame);
            AttachRopeKnotScripts(receiptBox);
            receiptBox.transform.SetParent(UI.instance.GetComponentInParent<Canvas>().transform, false);
            rcvItemWindowCG = receiptBox.GetComponent<CanvasGroup>();
            rcvItemWindowCG.alpha = 0f;
            rcvItemWindowCG.gameObject.SetActive(false);

            var inventory = AssetBundleManager.Load<Inventory>("prefabs/ui/inventory.prefab");
            rcvItemCard = Instantiate<InventoryItem>(inventory.itemPrefab);
            rcvItemCard.GetComponent<Button>().onClick.RemoveAllListeners();
            rcvItemCard.style = Inventory.Style.TwoColumn;
            rcvItemCard.transform.SetParent(receiptBox.transform, false);
            rcvItemItemCG = rcvItemCard.gameObject.AddComponent<CanvasGroup>();
            var rect = rcvItemCard.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.24f, 0.05f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);

            var rcvText = new GameObject("Received Text").AddComponent<WolText>();
            rcvText.text = "Received";
            rcvText.color = Color.black;
            rcvText.resizeTextForBestFit = true;
            rcvText.alignment = TextAnchor.MiddleLeft;
            rcvText.transform.SetParent(receiptBox.transform, false);
            var rcvTextRect = rcvText.GetComponent<RectTransform>();
            rcvTextRect.anchorMin = new Vector2(0.05f, 0.05f);
            rcvTextRect.anchorMax = new Vector2(0.22f, 0.95f);
            rcvTextRect.sizeDelta = Vector2.zero;
        }

        private void CreateAPConnectionBox()
        {
            WolapPlugin.Log.LogDebug("Setting up Archipelago connection settings window");

            var ropeFrame = WolapPlugin.Assets.LoadAsset<GameObject>(Constants.PluginAssetsPath + "/ui/ap connection box.prefab");

            WolapPlugin.Log.LogDebug("Setting up rope frame");
            var apConnectionGO = Instantiate(ropeFrame);
            AttachRopeKnotScripts(apConnectionGO);
            apConnectionGO.transform.SetParent(UI.instance.GetComponentInParent<Canvas>().transform, false);
            apConnectionBox = apConnectionGO.GetComponent<CanvasGroup>();

            WolapPlugin.Log.LogDebug("Loading dialog prompt");
            var dialogPrompt = AssetBundleManager.Load<DialogPrompt>("prefabs/ui/dialog_prompt_switch.prefab"); //dialog_prompt_switch has a WolInputField - dialog_prompt only has InputField for some reason

            var content = apConnectionBox.transform.Find("Content");

            WolapPlugin.Log.LogDebug("Setting up title row");
            var titleRow = content.Find("Title Row");
            var titleText = titleRow.gameObject.AddComponent<WolText>();
            titleText.text = "Archipelago Settings";
            titleText.color = Color.black;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMaxSize = 32;

            WolapPlugin.Log.LogDebug("Setting up first input row");
            var inputRows = content.Find("Input Rows");
            var firstRow = inputRows.GetChild(0);
            var firstLabelText = firstRow.GetChild(0).gameObject.AddComponent<WolText>();
            firstLabelText.text = "Slot:";
            firstLabelText.color = Color.black;
            firstLabelText.fontStyle = FontStyle.Bold;
            firstLabelText.alignment = TextAnchor.MiddleRight;
            firstLabelText.resizeTextForBestFit = true;
            firstLabelText.resizeTextMaxSize = 28;
            var dpInput = dialogPrompt.transform.Find("window/input");
            var inputCopy = Instantiate(dpInput, firstRow.GetChild(1).transform, false);
            var inputCopyRect = inputCopy as RectTransform;
            inputCopyRect.anchorMin = Vector2.zero;
            inputCopyRect.anchorMax = Vector2.one;
            var firstInput = firstRow.GetComponentInChildren<WolInputField>();
            ((Text)firstInput.placeholder).text = "";
            firstInput.characterLimit = 100;
            firstInput.gameObject.name = "AP Slot Input";

            var permaflags = MPlayer.instance.permaflags.data;
            if (permaflags.ContainsKey(Constants.APSettingsSlotFlag)) firstInput.text = permaflags[Constants.APSettingsSlotFlag];

            apConnectionInputs[0] = firstInput;

            WolapPlugin.Log.LogDebug("Setting up input row copies");
            for (int i = 0; i < 3; i++)
            {
                var newRow = Instantiate(firstRow, inputRows, false);
                var newLabel = newRow.GetChild(0).GetComponent<WolText>();
                var newInput = newRow.GetChild(1).GetComponentInChildren<WolInputField>();
                apConnectionInputs[i+1] = newInput;

                switch (i)
                {
                    case 0:
                        newLabel.text = "Host:";
                        newInput.gameObject.name = "AP Host Input";
                        if (permaflags.ContainsKey(Constants.APSettingsHostFlag)) newInput.text = permaflags[Constants.APSettingsHostFlag];
                        else newInput.text = "archipelago.gg";
                        ((Text)newInput.placeholder).text = "archipelago.gg";
                        break;
                    case 1:
                        newLabel.text = "Port:";
                        newInput.gameObject.name = "AP Port Input";
                        if (permaflags.ContainsKey(Constants.APSettingsPortFlag)) newInput.text = permaflags[Constants.APSettingsPortFlag];
                        newInput.contentType = WolInputField.ContentType.IntegerNumber;
                        break;
                    case 2:
                        newLabel.text = "Password:";
                        newInput.gameObject.name = "AP Password Input";
                        if (permaflags.ContainsKey(Constants.APSettingsPasswordFlag)) newInput.text = permaflags[Constants.APSettingsPasswordFlag];
                        newInput.contentType = WolInputField.ContentType.Password;
                        newInput.inputType = WolInputField.InputType.Password;
                        break;
                }
            }

            WolapPlugin.Log.LogDebug("Setting up connection row + button");
            var connRow = content.Find("Connection Row");
            var buttonContainer = connRow.GetChild(0);
            var wolButton = dialogPrompt.GetComponentInChildren<Button>();
            apConnectionButton = Instantiate(wolButton, buttonContainer, false);
            var buttonText = apConnectionButton.GetComponentInChildren<WolText>();
            buttonText.text = "Connect";
            apConnectionButton.onClick.RemoveAllListeners();
            apConnectionButton.onClick.AddListener(OnClickAPConnectionButton);
            var buttonRect = apConnectionButton.transform as RectTransform;
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            UpdateAPButtonClickable();

            WolapPlugin.Log.LogDebug("Setting up connection row text");
            apConnectionStatusText = connRow.GetChild(1).gameObject.AddComponent<WolText>();
            apConnectionStatusText.text = "Not Connected";
            apConnectionStatusText.color = Color.red;
            apConnectionStatusText.alignment = TextAnchor.MiddleCenter;
            apConnectionStatusText.resizeTextForBestFit = true;
            apConnectionStatusText.resizeTextMaxSize = 22;
        }

        private static void UpdateAPButtonClickable()
        {
            var slot = apConnectionInputs[0].text;
            var host = apConnectionInputs[1].text;
            var port = apConnectionInputs[2].text;
            if (slot.IsNullOrWhiteSpace() || host.IsNullOrWhiteSpace() || port.IsNullOrWhiteSpace()) apConnectionButton.interactable = false;
            else apConnectionButton.interactable = true;
        }

        private void AttachRopeKnotScripts(GameObject ropeFrame)
        {
            var knotParent = ropeFrame.transform.Find(Constants.GameObjectKnotsPath);
            if (knotParent == null)
            {
                WolapPlugin.Log.LogWarning("Could not find the rope knots on " + ropeFrame.name + " to attach the RopeKnot scripts.");
                return;
            }

            foreach (Transform knot in knotParent)
            {
                knot.gameObject.AddComponent<RopeKnot>();
            }
        }

        private void OnClickAPConnectionButton()
        {
            if (WolapPlugin.Archipelago.IsConnected) HandleDisconnectButtonClicked();
            else HandleConnectButtonClicked();
        }

        private void HandleConnectButtonClicked()
        {
            apConnectionButton.interactable = false;
            apConnectionStatusText.text = "Connecting...";
            apConnectionStatusText.color = Color.magenta;

            foreach (WolInputField input in apConnectionInputs)
            {
                input.interactable = false;
            }

            var slot = apConnectionInputs[0].text;
            var host = apConnectionInputs[1].text;
            var port = apConnectionInputs[2].text == "" ? 0 : Int32.Parse(apConnectionInputs[2].text);
            var pass = apConnectionInputs[3].text;

            WolapPlugin.Archipelago.CreateSession(host, port);
            StartCoroutine(ConnectRoutine(slot, pass));
        }

        private IEnumerator ConnectRoutine(string slot, string password)
        {
            ConnectionInProgress = true;

            Canvas.ForceUpdateCanvases();
            yield return new WaitForFixedUpdate();

            WolapPlugin.Archipelago.Connect(slot, password);

            apConnectionButton.interactable = true;
            if (WolapPlugin.Archipelago.IsConnected)
            {
                apConnectionButton.GetComponentInChildren<WolText>().text = "Disconnect";
                apConnectionStatusText.text = "Connected!";
                apConnectionStatusText.color = Color.blue;
            }
            else
            {
                apConnectionStatusText.text = "Connection Failed";
                apConnectionStatusText.color = Color.red;

                foreach (WolInputField input in apConnectionInputs)
                {
                    input.interactable = true;
                }
            }

            ConnectionInProgress = false;

            yield return null;
        }

        private void HandleDisconnectButtonClicked()
        {
            apConnectionButton.interactable = false;
            apConnectionStatusText.text = "Disconnecting...";
            apConnectionStatusText.color = Color.magenta;

            StartCoroutine(DisconnectRoutine());
        }

        private IEnumerator DisconnectRoutine()
        {
            ConnectionInProgress = true;

            Canvas.ForceUpdateCanvases();
            yield return new WaitForFixedUpdate();

            WolapPlugin.Archipelago.Disconnect();

            EnableEditingAPSettings();

            ConnectionInProgress = false;

            yield return null;
        }

        private static void EnableEditingAPSettings()
        {
            foreach (WolInputField input in apConnectionInputs)
            {
                input.interactable = true;
            }

            apConnectionButton.interactable = true;
            apConnectionButton.GetComponentInChildren<WolText>().text = "Connect";
            apConnectionStatusText.text = "Not Connected";
            apConnectionStatusText.color = Color.red;
        }

        private IEnumerator DisplayReceivedItemQueue()
        {
            rcvQueueInProgress = true;
            rcvItemItemCG.alpha = 1;

            while (RcvItemQueue.Count > 0)
            {
                RcvItemQueue.TryDequeue(out var item);

                if (rcvItemWindowCG.alpha == 1) yield return FadeOutCanvasGroup(rcvItemItemCG, rcvItemItemFadeTime);

                var mItem = ModelManager.GetItem(item.ID);
                rcvItemCard.item = mItem;
                rcvItemCard.sprite = SpriteLoader.SpriteForName(mItem.data["icon"]);
                rcvItemCard.quantity = item.Quantity;

                if (rcvItemItemCG.alpha == 0) yield return FadeInCanvasGroup(rcvItemItemCG, rcvItemItemFadeTime);

                if (rcvItemWindowCG.alpha == 0) yield return FadeInCanvasGroup(rcvItemWindowCG, rcvItemWindowFadeTime);

                yield return new WaitForSeconds(rcvItemShowTime);
            }

            yield return FadeOutCanvasGroup(rcvItemWindowCG, rcvItemWindowFadeTime);

            rcvQueueInProgress = false;
        }

        private void ToggleFadeCanvasGroup(CanvasGroup cg, float fadeTimeInSeconds)
        {
            if (rcvItemFading) return;

            if (cg.alpha >= 1) StartCoroutine(FadeOutCanvasGroup(cg, fadeTimeInSeconds));
            else if (cg.alpha <= 0) StartCoroutine(FadeInCanvasGroup(cg, fadeTimeInSeconds));
        }

        private IEnumerator FadeOutCanvasGroup(CanvasGroup cg, float fadeTimeInSeconds)
        {
            rcvItemFading = true;
            while (cg.alpha > 0f)
            {
                cg.alpha -= Time.unscaledDeltaTime / fadeTimeInSeconds;
                if (cg.alpha < 0f) cg.alpha = 0f;
                yield return null;
            }
            cg.gameObject.SetActive(false);
            rcvItemFading = false;
        }

        private IEnumerator FadeInCanvasGroup(CanvasGroup cg, float fadeTimeInSeconds)
        {
            rcvItemFading = true;
            cg.gameObject.SetActive(true);
            while (cg.alpha < 1f)
            {
                cg.alpha += Time.unscaledDeltaTime / fadeTimeInSeconds;
                if (cg.alpha > 1f) cg.alpha = 1f;
                yield return null;
            }
            rcvItemFading = false;
        }

        public void CloseReceivedItemQueue()
        {
            if (rcvQueueInProgress)
            { 
                rcvQueueInProgress = false;
                RcvItemQueue.Clear();
                if (rcvQueueCoroutine != null) WolapPlugin.UIManager.StopCoroutine(rcvQueueCoroutine);
                WolapPlugin.UIManager.StartCoroutine(FadeOutCanvasGroup(rcvItemWindowCG, 0.1f));
            }
        }

        //[HarmonyPatch(typeof(TitleStateWaa), "OnPush")]
        //[HarmonyPostfix]
        //private static void ShowAPSettingsOnTitlePatch()
        //{
        //    if (apConnectionBox != null)
        //    {
        //        EnableEditingAPSettings();
        //        apConnectionBox.gameObject.SetActive(true);
        //    }
        //}

        //[HarmonyPatch(typeof(TitleStateWaa), "OnPop")]
        //[HarmonyPostfix]
        //private static void HideAPSettingsOnLeaveTitlePatch()
        //{
        //    if (apConnectionBox != null) apConnectionBox.gameObject.SetActive(false);
        //}

        [HarmonyPatch(typeof(WolInputField), "OnSelect")]
        [HarmonyPostfix]
        private static void DisableControlsOnSelectPatch(BaseEventData eventData)
        {
            if (WestOfLoathing.instance.state_machine.IsState(TitleStateWaa.NAME))
            {
                Controls.PushDisableUIKeys();
            }
        }

        [HarmonyPatch(typeof(WolInputField), "OnDeselect")]
        [HarmonyPostfix]
        private static void EnableControlsOnDeselectPatch(BaseEventData eventData)
        {
            if (WestOfLoathing.instance.state_machine.IsState(TitleStateWaa.NAME))
            {
                Controls.PopDisableUIKeys();
            }
        }

        [HarmonyPatch(typeof(TitleStateWaa), "OnButton")]
        [HarmonyPrefix]
        private static bool DisableMenuButtonsDuringAPConnectionPatch(TitleStateWaa __instance, string str)
        {
            //Prevent title screen buttons (except for quit) from being clicked while connecting/disconnecting from AP
            return str == "quit" || !ConnectionInProgress;
        }

        [HarmonyPatch(typeof(WolInputField), "SendOnValueChanged")]
        [HarmonyPostfix]
        private static void ValidateInputsToMakeConnectButtonClickablePatch()
        {
            if (apConnectionButton == null) return;
            foreach (WolInputField field in apConnectionInputs) if (field == null) return;

            UpdateAPButtonClickable();
        }

        [HarmonyPatch(typeof(WolInputField), "SendOnSubmit")]
        [HarmonyPostfix]
        private static void SaveConnectionSettingsPatch(WolInputField __instance)
        {
            foreach (WolInputField field in apConnectionInputs) if (field == null) return;

            int i;
            for (i = 0; i < apConnectionInputs.Length; i++)
            {
                if (apConnectionInputs[i].gameObject.name == __instance.gameObject.name) break;
            }

            string permaflagKey;
            switch (i)
            {
                case 0:
                    permaflagKey = Constants.APSettingsSlotFlag;
                    break;
                case 1:
                    permaflagKey = Constants.APSettingsHostFlag;
                    break;
                case 2:
                    permaflagKey = Constants.APSettingsPortFlag;
                    break;
                case 3:
                    permaflagKey = Constants.APSettingsPasswordFlag;
                    break;
                default:
                    return;
            }

            MPlayer.instance.permaflags.data[permaflagKey] = __instance.text;
            SavedGame.SavePermaflags();
        }

        [HarmonyPatch(typeof(OptionsState), "OnMainMenu")]
        [HarmonyPrefix]
        private static void CloseReceivedItemsOnMainMenuPatch(OptionsState __instance)
        {
            WolapPlugin.UIManager.CloseReceivedItemQueue();
        }

        public struct ReceivedID(string id, int quantity)
        {
            public string ID { get; set; } = id;
            public int Quantity { get; set; } = quantity;
        }
    }
}
