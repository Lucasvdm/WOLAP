using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
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

        private const string GAMENAME = "West Of Loathing";

        private string hostname;
        private int port;
        private string password;
        private string slot;
        private ConcurrentQueue<ItemInfo> incomingItems;
        private List<long> outgoingLocations;

        public ArchipelagoClient()
        {
            incomingItems = new ConcurrentQueue<ItemInfo>();
            outgoingLocations = new List<long>();
        }

        public ArchipelagoClient(string hostname, int port = 38281) : this()
        {
            CreateSession(hostname, port);
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
                result = Session.TryConnectAndLogin(GAMENAME, slot, ItemsHandlingFlags.AllItems, requestSlotData: true, password: password);
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
            Session.Locations.AllLocationsChecked.First();
        }

        public void SendLocationCheck(string locationName)
        {
            if (locationName != null && IsConnected)
            {
                var locationId = Session.Locations.GetLocationIdFromName(Session.ConnectionInfo.Game, locationName);
                Session.Locations.CompleteLocationChecks(locationId);
            }
            else
            {
                WolapPlugin.Log.LogInfo("Sending check for this location (but not really): " + locationName); //Temporary
                //TODO: Logic for error handling and handling checks while disconnected (probably queuing them until reconnected)
            }
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
