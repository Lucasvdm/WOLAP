using BepInEx;
using System;
using System.Collections.Generic;
using System.Text;

namespace WOLAP
{
    internal class CheckLocationManager
    {
        public List<CheckLocation> LocationList { get; set; }

        public CheckLocationManager()
        {

        }

        public CheckLocation GetLocation(string locName)
        {
            if (locName.IsNullOrWhiteSpace()) return null;

            return LocationList.Find(loc => loc.Name == locName);
        }

        public void InsertCheckPickupCommands()
        {
            Dictionary<string, MScript> scripts = ModelManager.Instance.scripts;
            foreach (CheckLocation location in LocationList)
            {
                foreach (ScriptPath path in location.JsonCheckPaths)
                {
                    List<MCommand> stateCommands = scripts[path.scriptName].states[path.stateName].commands;
                    stateCommands.RemoveAt(path.index);
                    stateCommands.Insert(path.index, location.ScriptCommand);
                }
            }
        }

        public void InsertCheckMissCommands()
        {

        }
    }
}
