using System;
using System.Collections.Generic;
using System.Text;

namespace WOLAP
{
    internal class CheckLocation
    {
        public string Name { get; set; }
        public bool IsDLC { get; set; }

        public CheckLocation(string name, bool isDlc) { 
            this.Name = name;
            this.IsDLC = isDlc;
        }

        public CheckLocation(string name)
        {
            this.Name = name;
            this.IsDLC = false;
        }
    }
}
