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

namespace WOLAP
{
    internal class ArchipelagoClient
    {
        public ArchipelagoSession Session { get; private set; }
        public Dictionary<string, object> SlotData { get; private set; }
        public bool IsConnected { get { return Session != null && Session.Socket.Connected; } }

        private const string GAME_NAME = "West Of Loathing";
        private const string ITEM_RECEIVED_FLAG_PREFIX = "received_item_";

        private string hostname;
        private int port;
        private string password;
        private string slot;
        private ConcurrentQueue<ItemInfo> incomingItems;
        private List<string> outgoingLocations;

        public ArchipelagoClient()
        {
            incomingItems = new ConcurrentQueue<ItemInfo>();
            outgoingLocations = new List<string>();
        }

        public ArchipelagoClient(string hostname, int port = 38281) : this()
        {
            CreateSession(hostname, port);
        }

        public void Update()
        {
            if (!IsConnected) return;

            //Could handle this whole queue in a separate asynchronous method, but one item per frame should be fine
            if (incomingItems.Count > 0)
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
            WolapPlugin.Log.LogInfo($"Trying to create an Archipelago session with hostname {newHostname} and port {newPort}");
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

        public void Connect(string slot, string password)
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
                //Other data initialization and post-connection logic
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
        }

        private void ClearConnectionData()
        {
            //Resetting/clearing item lists, queues, and other vars for the multiworld slot data
            incomingItems.Clear();
            outgoingLocations.Clear();
        }

        private void HandleReceivedItem(ItemInfo item)
        {
            if (item == null) return;

            var flags = MPlayer.instance.data;

            if (flags.ContainsKey(ITEM_RECEIVED_FLAG_PREFIX + item.ItemName))
            {
                WolapPlugin.Log.LogInfo("Skipping incoming item " + item.ItemName + " in queue because it's already been received.");
                return;
            }
            else
            {
                //TODO: Give the item to the player. Probably need special case handling/callbacks for if it's received during various states, e.g. dialogue or when paused.
                flags.Add(ITEM_RECEIVED_FLAG_PREFIX + item.ItemName, "1");
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
    }
}
