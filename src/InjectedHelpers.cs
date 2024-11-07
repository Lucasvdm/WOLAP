using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WOLAP
{
    public class InjectedHelpers
    {
        public static void OverwriteScriptStates(Dictionary<string, MScript> scripts, MScript newScript)
        {
            if (scripts == null || newScript == null) return;

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
