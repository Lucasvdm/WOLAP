using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace WOLAP
{
    public class UIManager : MonoBehaviour
    {
        private bool isUiModified;

        private void Update()
        {
            if (!isUiModified && IsReadyToModifyUI())
            {
                isUiModified = true;
                SetUpUIChanges();
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
    }
}
