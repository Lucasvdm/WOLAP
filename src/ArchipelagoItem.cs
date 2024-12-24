using System;
using System.Collections.Generic;
using System.Text;

namespace WOLAP
{
    internal class ArchipelagoItem
    {
        public string Name { get; set; }
        public string[] IDs { get; set; }
        public bool IsDLC { get; set; }
        public int[] Quantities { get; set; }

        public ArchipelagoItem(string name, string[] ids, int[] quantities = null, bool isDlc = false)
        {
            this.Name = name;
            this.IDs = ids;
            this.Quantities = quantities == null ? [1] : quantities;
            this.IsDLC = isDlc;
        }
    }
}
