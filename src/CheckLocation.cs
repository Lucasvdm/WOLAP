using System;
using System.Collections.Generic;
using System.Text;

namespace WOLAP
{
    internal class CheckLocation
    {
        public string Name { get; set; }
        public MCommand ScriptCommand { get; set; }
        public ScriptPath[] JsonCheckPaths { get; set; }
        public ScriptPath[] JsonMissPaths { get; set; }
        public bool IsDLC { get; set; }

        public CheckLocation(string name, ScriptPath[] checkPaths, ScriptPath[] missPaths, bool isDlc) { 
            this.Name = name;
            this.ScriptCommand = new MCommand(MCommand.Op.STATESHARE, [name]); //STATESHARE is a Stadia-exclusive command which has no use, seems like the best dangling MCommand.Op value to hijack
            this.JsonCheckPaths = checkPaths;
            this.JsonMissPaths = missPaths;
            this.IsDLC = isDlc;
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
