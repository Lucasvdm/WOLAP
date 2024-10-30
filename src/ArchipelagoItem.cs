using System;
using System.Collections.Generic;
using System.Text;

namespace WOLAP.src
{
    internal class ArchipelagoItem
    {
        public string Name { get; set; }
        public string[] IDs { get; set; }
        public bool IsDLC { get; set; }
        public int[] Quantities { get; set; }

        public ArchipelagoItem(string name, string[] ids, bool isDlc)
        {
            this.Name = name;
            this.IDs = ids;
            this.IsDLC = isDlc;
        }

        public ArchipelagoItem(string name, string[] ids, int[] quantities = null, bool isDlc = false)
        {
            this.Name = name;
            this.IDs = ids;
            if (quantities == null) this.Quantities = [1];
            else this.Quantities = quantities;
            this.IsDLC = isDlc;
        }
    }
}
