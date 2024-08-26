using System;
using System.Collections.Generic;
using System.Text;

namespace WOLAP
{
    internal class CheckLocation
    {
        public string Name { get; set; }
        public MCommand ScriptCommand { get; set; }
        public bool IsDLC { get; set; }

        public CheckLocation(string name, bool isDlc) { 
            this.Name = name;
            this.ScriptCommand = new MCommand(MCommand.Op.STATESHARE, new[]{name}); //STATESHARE is a Stadia-exclusive command which has no use, seems like the best dangling MCommand.Op value to hijack
            this.IsDLC = isDlc;
        }
    }
}
