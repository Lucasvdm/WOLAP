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
            return UI.instance != null && WolapPlugin.Assets != null && WestOfLoathing.instance.state_machine.IsState(GameplayState.NAME);
        }

        //TODO: Define sizing using a HorizontalLayoutGroup or something on the parent frame instead of setting anchors in code, which would also allow for resizing the frame to fit longer/shorter item names
        private void SetUpUIChanges()
        {
            var ropeFrame = WolapPlugin.Assets.LoadAsset<GameObject>(Constants.PluginAssetsPath + "/ui/popup frame.prefab");

            var receiptBox = Instantiate(ropeFrame);
            AttachRopeKnotScripts(receiptBox);
            receiptBox.transform.SetParent(UI.instance.GetComponentInParent<Canvas>().transform, false);
            rcvItemWindowCG = receiptBox.GetComponent<CanvasGroup>();
            rcvItemWindowCG.alpha = 0f;

            var invs = Resources.FindObjectsOfTypeAll<Inventory>();
            var inventory = invs.First();
            rcvItemCard = Instantiate<InventoryItem>(inventory.itemPrefab);
            rcvItemCard.GetComponent<Button>().onClick.RemoveAllListeners();
            MItem item = ModelManager.GetItem("boots_barnaby");
            rcvItemCard.item = item;
            rcvItemCard.sprite = SpriteLoader.SpriteForName(item.data["icon"]);
            rcvItemCard.style = Inventory.Style.TwoColumn;
            rcvItemCard.quantity = 1;
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
            rcvItemFading = false;
        }

        private IEnumerator FadeInCanvasGroup(CanvasGroup cg, float fadeTimeInSeconds)
        {
            rcvItemFading = true;
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
