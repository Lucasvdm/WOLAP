using System;
using System.Collections.Generic;
using System.Linq;

namespace WOLAP
{
    public class InjectedHelpers
    {
        public static void OverwriteScriptStates(Dictionary<string, MScript> scripts, MScript newScript)
        {
            if (scripts == null || newScript == null) return;

            //Only load modded DLC scripts if the DLC is enabled in the Archipelago options
            //This object -> string -> int -> boolean conversion is gross, but necessary
            if (WolapPlugin.Archipelago.SlotData.TryGetValue(Constants.DlcEnabledSlotDataFlag, out object dlcEnabled) && !Convert.ToBoolean(int.Parse(dlcEnabled.ToString())) && newScript.id.StartsWith("house_")) return;

            if (scripts.Keys.Contains(newScript.id))
            {
                MScript origScript = scripts[newScript.id];
                var stateIDs = newScript.states.Keys.ToArray();
                foreach (string stateID in stateIDs)
                {
                    origScript.states[stateID] = newScript.states[stateID];
                }
            }
            else
            {
                scripts.Add(newScript.id, newScript);
            }
        }
    }
}
