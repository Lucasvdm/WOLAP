using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WOLAP
{
    internal class ArchipelagoClient
    {
        public ArchipelagoSession Session { get; private set; }
        public Dictionary<string, object> SlotData { get; private set; }
        public bool IsConnected { get { return Session != null && Session.Socket.Connected; } }
        public bool SlotDataFlagsSet { get; set; }

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

        private bool shouldLogDisconnect = false;

        public ArchipelagoClient()
        {
            incomingItems = new ConcurrentQueue<ItemInfo>();
            outgoingLocations = new List<string>();
            itemManager = new ArchipelagoItemManager();
            SlotData = new Dictionary<string, object>();
        }

        public ArchipelagoClient(string hostname, int port = 38281) : this()
        {
            CreateSession(hostname, port);
        }

        public void Update()
        {
            if (!IsConnected)
            {
                if (shouldLogDisconnect)
                {
                    WolapPlugin.Log.LogWarning("Disconnected from Archipelago server!");
                    shouldLogDisconnect = false;
                }

                return;
            }

            if (IsInItemGrantableState())
            {
                if (!MPlayer.instance.data.ContainsKey(Constants.StartingItemsGrantedFlag))
                {
                    var startingInventory = GetStartingInventory();
                    if (startingInventory.Count == 0)
                    {
                        MPlayer.instance.data.Add(Constants.StartingItemsEmptyFlag, "1");
                    }
                    else
                    {
                        foreach (ItemInfo item in GetStartingInventory())
                        {
                            HandleReceivedItem(item);
                        }
                    }
                    
                    MPlayer.instance.data.Add(Constants.StartingItemsGrantedFlag, "1");
                }

                if (!SlotDataFlagsSet)
                {
                    UpdateFlagsFromSlotData();
                    SlotDataFlagsSet = true;
                }
                
                //Could handle this whole queue in a separate asynchronous method, but one item per frame should be fine
                if (incomingItems.Count > 0)
                {
                    incomingItems.TryDequeue(out var item);
                    HandleReceivedItem(item);
                }
            }
            
            if (outgoingLocations.Count > 0) SendOutgoingChecks();

            if (IsGameComplete() && !MPlayer.instance.data.ContainsKey(Constants.SentGameCompletionFlag)) SendGameCompletion();
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
                result = Session.TryConnectAndLogin(Constants.GameName, slot, ItemsHandlingFlags.AllItems, requestSlotData: true, password: password);
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

        public void TryReconnect()
        {
            if (Session == null || slot.IsNullOrWhiteSpace()) return;

            WolapPlugin.Log.LogInfo("Attempting to reconnect to Archipelago...");

            Connect(slot, password);
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            if (Session != null)
            {
                Session.Socket.DisconnectAsync().Wait(TimeSpan.FromSeconds(5));
                Session = null;
            }

            ClearConnectionData();

            WolapPlugin.Log.LogInfo("Disconnected from Archipelago server.");
            shouldLogDisconnect = false;
        }

        private void ClearConnectionData()
        {
            //Resetting/clearing item lists, queues, and other vars for the multiworld slot data
            incomingItems.Clear();
            outgoingLocations.Clear();
            SlotData.Clear();

            SlotDataFlagsSet = false;
            shouldLogDisconnect = true;

            foreach (ShopCheckLocation check in ShopCheckLocations) check.ApItemInfo = null;
        }

        public void ResetItemManager()
        {
            itemManager.ResetItemCounts();
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
            string itemReceivedFlag = Constants.ItemReceivedFlagPrefix + item.ItemName.Replace(" ", "");
            string itemReceivedFlagIndexed = itemReceivedFlag + "_" + index;

            if (!flags.ContainsKey(itemReceivedFlagIndexed))
            {
                WolapPlugin.Log.LogInfo($"Received {item.ItemDisplayName} from {item.Player} at {item.LocationDisplayName}");

                if (!itemManager.ProcessItem(item.ItemName))
                {
                    WolapPlugin.Log.LogWarning($"Failed to process item: {item.ItemDisplayName}");
                    return;
                }

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
                MPlayer.instance.data.Add(Constants.GotCheckFlagPrefix + locationName.Replace(" ", ""), "1");
            }
            else
            {
                WolapPlugin.Log.LogInfo($"Tried to send check for location {locationName} but not connected to Archipelago, adding to outgoing list.");

                outgoingLocations.Add(locationName); //TODO: Also add these as flags to populate the list next time if the game is closed before reconnecting
            }
        }

        private void SendOutgoingChecks()
        {
            var ids = new long[outgoingLocations.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = Session.Locations.GetLocationIdFromName(Session.ConnectionInfo.Game, outgoingLocations[i]);
                MPlayer.instance.data.Add(Constants.GotCheckFlagPrefix + outgoingLocations[i].Replace(" ", ""), "1");
            }
            Session.Locations.CompleteLocationChecks(ids);
        }

        private bool IsGameComplete()
        {
            return WestOfLoathing.instance.state_machine.IsState(GameplayState.NAME) && MPlayer.instance.data.ContainsKey("endcredits");
        }

        private void SendGameCompletion()
        {
            if (!IsConnected) return;

            Session.SetGoalAchieved();
            WolapPlugin.Log.LogInfo("Game completed! Sent completion to AP server.");

            MPlayer.instance.data[Constants.SentGameCompletionFlag] = "1";
        }
         
        public List<ItemInfo> GetStartingInventory()
        {
            List<ItemInfo> startingInventory = [];
            if (IsConnected)
            {
                //Starting inventory items have a location ID of -2
                foreach (ItemInfo item in Session.Items.AllItemsReceived)
                {
                    if (item.LocationId == -2) startingInventory.Add(item);
                }
            }
            return startingInventory;
        }

        private void UpdateFlagsFromSlotData()
        {
            UpdateSlotDataFlag(Constants.DlcEnabledSlotDataFlag);
            UpdateSlotDataFlag(Constants.RandomizeGhostCoachFlag);
            UpdateSlotDataFlag(Constants.RandomizeGoblintongueFlag);
        }

        private void UpdateSlotDataFlag(string name)
        {
            var flags = MPlayer.instance.data;

            WolapPlugin.Log.LogInfo($"Slot data flag [{name}]: {SlotData[name]}");

            if (flags.ContainsKey(name)) flags[name] = SlotData[name].ToString();
            else flags.Add(name, SlotData[name].ToString());
        }

        public void PopulateShopCheckItemInfo()
        {
            PopulateShopCheckItemInfo(ShopCheckLocations);
        }

        public void PopulateShopCheckItemInfo(List<ShopCheckLocation> locationsToPopulate)
        {
            List<long> checkIDs = locationsToPopulate.Select(check => Session.Locations.GetLocationIdFromName(Constants.GameName, check.Name)).ToList();

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

        public void AddMissingInitialChecksToShops()
        {
            Dictionary<string, string> flags = MPlayer.instance.data;

            List<ShopCheckLocation> checksToAdd = new List<ShopCheckLocation>();
            foreach (ShopCheckLocation check in ShopCheckLocations)
            {
                if (!flags.ContainsKey(Constants.AddedShopCheckFlagPrefix + check.Name.Replace(" ", "")) && (check.IsAddableEarly || flags.ContainsKey(Constants.UnlockedShopCheckFlagPrefix + check.Name.Replace(" ", ""))))
                {
                    checksToAdd.Add(check);
                }
            }

            AddChecksToShops(checksToAdd);
        }

        public void AddChecksToShops(List<ShopCheckLocation> checks)
        {
            foreach (ShopCheckLocation check in checks)
            {
                AddCheckToShop(check);
            }
        }

        public MItem AddCheckToShop(ShopCheckLocation check)
        {
            if (check.ApItemInfo == null) return null;

            MItem baseItem = ModelManager.GetItem(Constants.ShopCheckItemID);
            if (baseItem == null)
            {
                WolapPlugin.Log.LogError("Couldn't find the base archipelago shop check item!");
                return null;
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

            MPlayer.instance.data.Add(Constants.AddedShopCheckFlagPrefix + check.Name.Replace(" ", ""), "1");

            WolapPlugin.Log.LogInfo($"Added check [{check.Name}] to shop [{check.ShopID}]");

            return checkItem;
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
