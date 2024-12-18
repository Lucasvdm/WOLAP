using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace WOLAP
{
    internal class ShopCheckLocation : CheckLocation
    {
        public string ShopID { get; set; }
        public int Price { get; set; }
        public bool IsAddableEarly { get; set; } //For checks that can be added to shop inventory from the start, instead of waiting to be unlocked
        public ItemInfo ApItemInfo { get; set; }
        
        public ShopCheckLocation(string name, string shopId, int price, bool addableEarly = true, bool isDlc = false) : base(name, isDlc)
        {
            this.ShopID = shopId;
            this.Price = price;
            this.IsAddableEarly = addableEarly;
        }
    }
}
