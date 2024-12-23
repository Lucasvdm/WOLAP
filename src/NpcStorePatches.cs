using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace WOLAP
{
    [HarmonyPatch]
    internal class NpcStorePatches
    {
        [HarmonyPatch(typeof(NpcStore), "HandleBuySell")]
        [HarmonyPrefix]
        static bool HandleBuySellAPCheckPatch(NpcStore __instance)
        {
            NpcStoreItem npcStoreItem = NpcStoreItem.selected;
            if (npcStoreItem == null) return false;

            MItem storeItem = npcStoreItem.item;
            if (storeItem.id != "archipelago_shopitem") return true;

            if (!MPlayer.instance.FTrySubtractMeat(npcStoreItem.meat))
            {
                WestOfLoathing.instance.state_machine.Push(new ConfirmState("Not enough Meat", "You don't have enough Meat to buy that.", "Dang", null, null));
                return false;
            }
            SoundPlayer.instance.PlayUISound(UISound.STOREBUYITEM);
            Store.current.RemoveItem(storeItem.invid, 1);

            WolapPlugin.Archipelago.SendLocationCheck(storeItem.data["source"]);

            Traverse traverse = Traverse.Create(__instance);
            traverse.Method("PostBuySell", [typeof(int), typeof(int), typeof(bool), typeof(bool)], [storeItem.invid, -1, true, false]).GetValue();

            return false;
        }
    }
}
