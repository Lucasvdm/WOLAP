using Archipelago.MultiClient.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace WOLAP
{
    internal class ArchipelagoClient
    {
        public ArchipelagoSession Session { get; private set; }
        public bool IsConnected { get { return Session != null && Session.Socket.Connected; } }

        public ArchipelagoClient(string hostname, int port = 38281)
        {
            Session = ArchipelagoSessionFactory.CreateSession(hostname, port);
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
    }
}
