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
                foreach (string stateID in origScript.states.Keys)
                {
                    if (newScript.states.Keys.Contains(stateID))
                    {
                        origScript.states[stateID].Clear();
                        origScript.states[stateID] = newScript.states[stateID];
                    }
                }
            }
            else
            {
                scripts.Add(newScript.id, newScript);
            }
        }
    }
}
