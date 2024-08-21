using System;
using System.Collections.Generic;
using System.Text;

namespace WOLAP
{
    internal class CheckLocation
    {
        public string Name { get; set; }
        public MCommand ScriptCommand { get; set; }
        public List<ScriptPath> JsonCheckPaths { get; set; }
        public List<ScriptPath> JsonMissPaths { get; set; }

        public CheckLocation(string name, List<ScriptPath> checkPaths, List<ScriptPath> missPaths) { 
            this.Name = name;
            this.ScriptCommand = new MCommand(MCommand.Op.STATESHARE, [name]); //STATESHARE is a Stadia-exclusive command which has no use, seems like the best dangling MCommand.Op value to hijack
            this.JsonCheckPaths = checkPaths;
            this.JsonMissPaths = missPaths;
        }
    }

    public struct ScriptPath
    {
        public string scriptName;
        public string stateName;
        public int index;

        public ScriptPath(string script, string state, int index)
        {
            this.scriptName = script;
            this.stateName = state;
            this.index = index;
        }
    }
}
