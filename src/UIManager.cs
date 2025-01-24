using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace WOLAP
{
    public class UIManager : MonoBehaviour
    {
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

        private CanvasGroup apConnectionBox;

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
                if (RcvItemQueue.Count > 0 && !rcvQueueInProgress) StartCoroutine(DisplayReceivedItemQueue());
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
            rect.anchorMin = new Vector2(0.3f, 0.05f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);

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

            WolapPlugin.Log.LogDebug("Setting up input row copies");
            for (int i = 0; i < 3; i++)
            {
                var newRow = Instantiate(firstRow, inputRows, false);
                var newLabel = newRow.GetChild(0).GetComponent<WolText>();
                var newInput = newRow.GetChild(1).GetComponentInChildren<WolInputField>();

                switch (i)
                {
                    case 0:
                        newLabel.text = "Host:";
                        newInput.text = "archipelago.gg";
                        ((Text)newInput.placeholder).text = "archipelago.gg";
                        break;
                    case 1:
                        newLabel.text = "Port:";
                        break;
                    case 2:
                        newLabel.text = "Password:";
                        break;
                }
            }

            WolapPlugin.Log.LogDebug("Setting up connection row + button");
            var connRow = content.Find("Connection Row");
            var buttonContainer = connRow.GetChild(0);
            var wolButton = dialogPrompt.GetComponentInChildren<Button>();
            var newButton = Instantiate(wolButton, buttonContainer, false);
            var buttonText = newButton.GetComponentInChildren<WolText>();
            buttonText.text = "Connect";
            newButton.onClick.RemoveAllListeners();
            var buttonRect = newButton.transform as RectTransform;
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;

            WolapPlugin.Log.LogDebug("Setting up connection row text");
            var connText = connRow.GetChild(1).gameObject.AddComponent<WolText>();
            connText.text = "Not Connected";
            connText.color = Color.red;
            connText.alignment = TextAnchor.MiddleCenter;
            connText.resizeTextForBestFit = true;
            connText.resizeTextMaxSize = 22;
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

        private IEnumerator DisplayReceivedItemQueue()
        {
            rcvQueueInProgress = true;

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
                cg.alpha -= Time.deltaTime / fadeTimeInSeconds;
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
                cg.alpha += Time.deltaTime / fadeTimeInSeconds;
                if (cg.alpha > 1f) cg.alpha = 1f;
                yield return null;
            }
            rcvItemFading = false;
        }

        public struct ReceivedID(string id, int quantity)
        {
            public string ID { get; set; } = id;
            public int Quantity { get; set; } = quantity;
        }
    }
}
