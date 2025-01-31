using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WOLAP
{
    internal static class Constants
    {
        public const KeyCode DialogDebugToggleKey = KeyCode.Backslash;

        public const string GameName = "West of Loathing";

        public const string PluginGuid = "lucasvdm.westofloathing.aprandomizer";
        public const string PluginName = "West of Loathing Archipelago Randomizer";
        public const string PluginNameShort = "WOLAP";
        public const string PluginVersion = "0.1.0";
        public const string PluginAssetsPath = "assets/wolap_assets";

        public const string ItemReceivedFlagPrefix = "received_item_";

        public const string UnlockedShopCheckFlagPrefix = "unlocked_shop_check_";
        public const string AddedShopCheckFlagPrefix = "added_shop_check_";
        public const string ShopCheckItemID = "archipelago_shopitem";

        public const string ModdedSaveProperty = "archipelago_save";

        public const string NexmexCountFlag = "nexmex_books_found";

        public const string SentGameCompletionFlag = "archipelago_game_complete";

        public const string APSettingsSlotFlag = "archipelago_settings_slot";
        public const string APSettingsHostFlag = "archipelago_settings_host";
        public const string APSettingsPortFlag = "archipelago_settings_port";
        public const string APSettingsPasswordFlag = "archipelago_settings_password";

        public const string GameObjectKnotsPath = "Rope Border/Knots";
    }
}
