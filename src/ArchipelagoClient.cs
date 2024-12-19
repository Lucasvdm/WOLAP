using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using BepInEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static PathMaker;

namespace WOLAP
{
    internal class ArchipelagoClient
    {
        public ArchipelagoSession Session { get; private set; }
        public Dictionary<string, object> SlotData { get; private set; }
        public bool IsConnected { get { return Session != null && Session.Socket.Connected; } }

        public const string GAME_NAME = "West of Loathing";
        public const string ITEM_RECEIVED_FLAG_PREFIX = "received_item_";
        public const string UNLOCKED_SHOP_CHECK_FLAG_PREFIX = "unlocked_shop_check_";
        public const string ADDED_SHOP_CHECK_FLAG_PREFIX = "added_shop_check_";
        public const string SHOP_CHECK_ITEM_TEMPLATE_NAME = "archipelago_shopitem";

        private string hostname;
        private int port;
        private string password;
        private string slot;
        private ConcurrentQueue<ItemInfo> incomingItems;
        private List<string> outgoingLocations;
        private ArchipelagoItemManager itemManager;
        private List<string> itemGrantableStateNames = [
            //CombatState.NAME,
            GameplayState.NAME,
            InventoryState.NAME
        ];

        public ArchipelagoClient()
        {
            incomingItems = new ConcurrentQueue<ItemInfo>();
            outgoingLocations = new List<string>();
            itemManager = new ArchipelagoItemManager();
        }

        public ArchipelagoClient(string hostname, int port = 38281) : this()
        {
            CreateSession(hostname, port);
        }

        public void Update()
        {
            if (!IsConnected) return; //TODO: Attempt reconnection?

            //Could handle this whole queue in a separate asynchronous method, but one item per frame should be fine
            if (incomingItems.Count > 0 && IsInItemGrantableState())
            {
                incomingItems.TryDequeue(out var item);
                HandleReceivedItem(item);
            }

            if (outgoingLocations.Count > 0) SendOutgoingChecks();
        }

        public void CreateSession()
        {
            CreateSession(hostname, port);
        }

        public void CreateSession(string newHostname, int newPort)
        {
            WolapPlugin.Log.LogInfo($"Trying to create an Archipelago session with hostname {newHostname} and port {newPort}.");
            if (newHostname.IsNullOrWhiteSpace() || newPort == 0)
            {
                WolapPlugin.Log.LogError("A valid hostname and port are required to create an Archipelago session.");
                return;
            }

            if (IsConnected) Session.Socket.DisconnectAsync();

            Session = ArchipelagoSessionFactory.CreateSession(newHostname, newPort);

            if (Session != null)
            {
                hostname = newHostname;
                port = newPort;
                WolapPlugin.Log.LogInfo("Archipelago session created.");

                Session.Items.ItemReceived += (receivedItemsHelper) => {
                    incomingItems.Enqueue(receivedItemsHelper.DequeueItem());
                };
            }
        }

        public void Connect(string slot, string password = "")
        {
            if (IsConnected) return;

            if (Session == null)
            {
                if (hostname.IsNullOrWhiteSpace() || port == 0)
                {
                    WolapPlugin.Log.LogWarning("An Archipelago session with a valid hostname and port needs to be created before connecting to the server.");
                    return;
                }
                else
                {
                    CreateSession();
                }
            }

            ClearConnectionData();

            LoginResult result;
            try
            {
                result = Session.TryConnectAndLogin(GAME_NAME, slot, ItemsHandlingFlags.AllItems, requestSlotData: true, password: password);
            }
            catch (Exception ex)
            {
                result = new LoginFailure(ex.GetBaseException().Message);
            }

            if (result is LoginSuccessful success)
            {
                WolapPlugin.Log.LogInfo("Connected to Archipelago server.");

                this.slot = slot;
                this.password = password;
                SlotData = success.SlotData;

                PopulateShopCheckItemInfo();
                Dictionary<string, string> flags = MPlayer.instance.data;
                AddChecksToShops(ShopCheckLocations.FindAll(check => check.ApItemInfo != null && !flags.ContainsKey(ADDED_SHOP_CHECK_FLAG_PREFIX + check.Name.Replace(" ", ""))));
            }
            else
            {
                LoginFailure failure = (LoginFailure)result;

                WolapPlugin.Log.LogError("Failed to connect to Archipelago server.");
                foreach (ConnectionRefusedError error in failure.ErrorCodes)
                {
                    WolapPlugin.Log.LogError("Error code: " + error.ToString());
                }
                string errorText = failure.Errors.Length > 0 ? String.Join("\n", failure.Errors) : "No errors listed, check your connection settings.";
                WolapPlugin.Log.LogError(errorText);
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            if (Session != null)
            {
                Session.Socket.DisconnectAsync();
                Session = null;
            }

            ClearConnectionData();

            WolapPlugin.Log.LogInfo("Disconnected from Archipelago server.");
        }

        private void ClearConnectionData()
        {
            //Resetting/clearing item lists, queues, and other vars for the multiworld slot data
            incomingItems.Clear();
            outgoingLocations.Clear();
        }

        private bool IsInItemGrantableState()
        {
            string name = WestOfLoathing.instance.state_machine.state.name;
            return name != null && itemGrantableStateNames.Contains(name);
        }

        private void HandleReceivedItem(ItemInfo item)
        {
            if (item == null) return;

            //All previously collected items are received every time you connect, so filter out old items
            var flags = MPlayer.instance.data;
            int index = Session.Items.AllItemsReceived.IndexOf(item);
            string itemReceivedFlag = ITEM_RECEIVED_FLAG_PREFIX + item.ItemName.Replace(" ", "");
            string itemReceivedFlagIndexed = itemReceivedFlag + "_" + index;

            if (!flags.ContainsKey(itemReceivedFlagIndexed))
            {
                WolapPlugin.Log.LogInfo($"Received {item.ItemDisplayName} from {item.Player} at {item.LocationDisplayName}");

                bool received = itemManager.ProcessItem(item.ItemName);
                flags.Add(itemReceivedFlagIndexed, "1");

                //Unindexed flag is used for checking with hasflag in JSON
                if (flags.ContainsKey(itemReceivedFlag)) flags[itemReceivedFlag] = (int.Parse(flags[itemReceivedFlag]) + 1).ToString(); //Increment flag
                else flags.Add(itemReceivedFlag, "1");
            }
        }

        public void SendLocationCheck(string locationName)
        {
            if (locationName == null) return;

            if (IsConnected)
            {
                var locationId = Session.Locations.GetLocationIdFromName(Session.ConnectionInfo.Game, locationName);
                Session.Locations.CompleteLocationChecks(locationId);
            }
            else
            {
                WolapPlugin.Log.LogInfo($"Tried to send check for location {locationName} but not connected to Archipelago, adding to outgoing list.");

                outgoingLocations.Add(locationName);
            }
        }

        private void SendOutgoingChecks()
        {
            var ids = new long[outgoingLocations.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = Session.Locations.GetLocationIdFromName(Session.ConnectionInfo.Game, outgoingLocations[i]);
            }
            Session.Locations.CompleteLocationChecks(ids);
        }
         
        public List<string> GetStartingInventory()
        {
            List<string> startingInventory = new List<string>();
            if (IsConnected)
            {
                //Starting inventory items have a location ID of -2
                foreach (ItemInfo item in Session.Items.AllItemsReceived)
                {
                    if (item.LocationId == -2) startingInventory.Add(item.ItemName);
                }
            }
            return startingInventory;
        }

        public void PopulateShopCheckItemInfo()
        {
            PopulateShopCheckItemInfo(ShopCheckLocations);
        }

        public void PopulateShopCheckItemInfo(List<ShopCheckLocation> locationsToPopulate)
        {
            Dictionary<string, string> flags = MPlayer.instance.data;

            List<long> checkIDs = new List<long>();
            foreach (ShopCheckLocation check in ShopCheckLocations)
            {
                if (!flags.ContainsKey(ADDED_SHOP_CHECK_FLAG_PREFIX + check.Name.Replace(" ", "")) && (check.IsAddableEarly || flags.ContainsKey(UNLOCKED_SHOP_CHECK_FLAG_PREFIX + check.Name.Replace(" ", ""))))
                {
                    checkIDs.Add(Session.Locations.GetLocationIdFromName(GAME_NAME, check.Name));
                }
            }

            Session.Locations.ScoutLocationsAsync(checkIDs.ToArray()).ContinueWith(locationInfoPacket =>
            {
                foreach (ItemInfo itemInfo in locationInfoPacket.Result.Values)
                {
                    ShopCheckLocation location = ShopCheckLocations.Find(check => check.Name == itemInfo.LocationName);
                    location.ApItemInfo = itemInfo;
                }
            }).Wait(TimeSpan.FromSeconds(10));

            WolapPlugin.Log.LogInfo("Retrieved item info for addable shop check locations.");
        }

        public void AddChecksToShops(List<ShopCheckLocation> checks)
        {
            foreach (ShopCheckLocation check in checks)
            {
                AddCheckToShop(check);
            }
        }

        public void AddCheckToShop(ShopCheckLocation check)
        {
            if (check.ApItemInfo == null) return; //TODO: Make a call to retrieve item info here

            MItem baseItem = ModelManager.GetItem(SHOP_CHECK_ITEM_TEMPLATE_NAME);
            if (baseItem == null)
            {
                WolapPlugin.Log.LogError("Couldn't find the base archipelago shop check item!");
                return;
            }

            ItemInfo info = check.ApItemInfo;
            MItem checkItem = baseItem.Clone();
            checkItem.data["name"] = info.ItemDisplayName;
            
            string description = $"{info.Player.Name}'s {info.ItemDisplayName} ";
            if (info.Flags.HasFlag(ItemFlags.Advancement)) description += "(Progression)";
            else if (info.Flags.HasFlag(ItemFlags.NeverExclude)) description += "(Useful)";
            else if (info.Flags.HasFlag(ItemFlags.None)) description += "(Filler)";
            checkItem.data["description"] = description;
            checkItem.data["source"] = check.Name;

            Store.AddStockItem(check.ShopID, checkItem, 1, check.Price);

            MPlayer.instance.data.Add(ADDED_SHOP_CHECK_FLAG_PREFIX + check.Name.Replace(" ", ""), "1");

            WolapPlugin.Log.LogInfo($"Added check [{check.Name}] to shop [{check.ShopID}]");
        }

        public static readonly List<ShopCheckLocation> ShopCheckLocations = new List<ShopCheckLocation>
        {
            new ShopCheckLocation("Dirtwater Mercantile - Item 1", "dirtwatergeneral", 500),
            new ShopCheckLocation("Dirtwater Mercantile - Item 2", "dirtwatergeneral", 1000),
            new ShopCheckLocation("Dirtwater (Tony's Boots) - Item 1", "tonysboots", 1000),
            new ShopCheckLocation("Dirtwater (Tony's Boots) - Item 2", "tonysboots", 1000),
            new ShopCheckLocation("Dirtwater (Tony's Boots) - Item 3", "tonysboots", 1000),
            new ShopCheckLocation("Dirtwater (Tony's Boots) - Bonus Item", "tonysboots", 510, false),
            new ShopCheckLocation("Dirtwater (Murray's Curiosity & Bean) - Item 1", "curiosity", 300),
            new ShopCheckLocation("Dirtwater (Murray's Curiosity & Bean) - Item 2", "curiosity", 1000),
            new ShopCheckLocation("Dirtwater (Murray's Curiosity & Bean) - Item 3", "curiosity", 650),
            new ShopCheckLocation("Dirtwater (Murray's Curiosity & Bean) - Item 4", "curiosity", 500),
            new ShopCheckLocation("Dirtwater (Murray's Curiosity & Bean) - Bonus Item", "curiosity", 366, false),
            new ShopCheckLocation("Dirtwater (Grady's Fine Leather Goods) - Item 1", "tanner", 500),
            new ShopCheckLocation("Dirtwater (Grady's Fine Leather Goods) - Item 2", "tanner", 400),
            new ShopCheckLocation("Dirtwater (Grady's Fine Leather Goods) - Item 3", "tanner", 400),
            new ShopCheckLocation("Dirtwater (Grady's Fine Leather Goods) - Bonus Item", "tanner", 636, false),
            new ShopCheckLocation("Dirtwater (Alexandria's Bookstore) - Item 1", "bookstore", 500),
            new ShopCheckLocation("Dirtwater (Alexandria's Bookstore) - Item 2", "bookstore", 1500),
            new ShopCheckLocation("Dirtwater (Alexandria's Bookstore) - Item 3", "bookstore", 1500),
            new ShopCheckLocation("Dirtwater (Alexandria's Bookstore) - Item 4", "bookstore", 1500),
            new ShopCheckLocation("Dirtwater (Alexandria's Bookstore) - Item 5", "bookstore", 500),
            new ShopCheckLocation("Dirtwater (Alexandria's Bookstore) - Item 6", "bookstore", 500),
            new ShopCheckLocation("Dirtwater (Alexandria's Bookstore) - Item 7", "bookstore", 1500),
            new ShopCheckLocation("Dirtwater (Alexandria's Bookstore) - Bonus Item", "bookstore", 1500, false),
            new ShopCheckLocation("Buttonwillow's Store - Item 1", "buttonwillow", 300),
            new ShopCheckLocation("Buttonwillow's Store - Item 2", "buttonwillow", 60),
            new ShopCheckLocation("Buttonwillow's Store - Item 3", "buttonwillow", 200),
            new ShopCheckLocation("Buttonwillow's Store - Item 4", "buttonwillow", 1000),
            new ShopCheckLocation("Buttonwillow's Store - Item 5", "buttonwillow", 1000),
            new ShopCheckLocation("Buttonwillow's Store - Item 6", "buttonwillow", 1500),
            new ShopCheckLocation("Buttonwillow's Store - Item 7", "buttonwillow", 30),
            new ShopCheckLocation("Dynamite Dan's - Item 1", "dynamitedan", 5000),
            new ShopCheckLocation("Rescue Mission - Shop Item 1", "mission1", 60),
            new ShopCheckLocation("Rescue Mission - Shop Item 2", "mission1", 200),
            new ShopCheckLocation("Breadwood Trading Post - Item 1", "breadwood", 700),
            new ShopCheckLocation("Breadwood Trading Post - Item 2", "breadwood", 10),
            new ShopCheckLocation("Breadwood Trading Post - Item 3", "breadwood", 1000),
            new ShopCheckLocation("Breadwood Trading Post - Item 4", "breadwood", 2500),
            new ShopCheckLocation("Breadwood Trading Post - Item 5", "breadwood", 30),
            new ShopCheckLocation("Breadwood Trading Post - Item 6", "breadwood", 30),
            new ShopCheckLocation("Breadwood Trading Post - Item 7", "breadwood", 1000),
            new ShopCheckLocation("Breadwood Trading Post - Item 8", "breadwood", 300),
            new ShopCheckLocation("Breadwood Trading Post - Item 9", "breadwood", 30),
            new ShopCheckLocation("Breadwood Trading Post - Item 10", "breadwood", 400),
            new ShopCheckLocation("Halloway's Hideaway - Shop Item 1", "halloway", 65),
            new ShopCheckLocation("Halloway's Hideaway - Shop Item 2", "halloway", 200),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 1", "sally", 15),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 2", "sally", 50),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 3", "sally", 20),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 4", "sally", 100),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 5", "sally", 50),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 6", "sally", 100),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 7", "sally", 1000),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 8", "sally", 300),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 9", "sally", 10, false),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 10", "sally", 100, false),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 11", "sally", 200, false),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 12", "sally", 1500, false),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 13", "sally", 100, false),
            new ShopCheckLocation("Wanderin' Sally's Camp - Item 14", "sally", 1000, false)
        };
    }
}
