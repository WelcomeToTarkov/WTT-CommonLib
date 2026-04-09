using Comfort.Common;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using ProductionScheme = GClass2440;

namespace WTTClientCommonLib.Models;

public class ExtendedProductionScheme : ProductionScheme
{
    private bool _itemsLoaded = false;
    
    [JsonProperty("endProductItems")]
    public FlatItemsDataClass[] EndProductItems { get; set; }
    
    public List<Item> ResultItems = [];
    public List<Item> BaseItems = [];

    public void LoadResultItems()
    {
        if (!_itemsLoaded)
        {
            List<string> baseItemTpls = [];
            foreach (var flatItem in EndProductItems)
            {
                if (flatItem.slotId == String.Empty)
                {
                    baseItemTpls.Add(flatItem._tpl);
                }
            }
            
            var items = Singleton<ItemFactoryClass>.Instance.FlatItemsToTree(EndProductItems).Items;
            foreach ((string id, Item item) in items)
            {
                ResultItems.Add(item);

                if (baseItemTpls.Contains(item.StringTemplateId))
                {
                    BaseItems.Add(item);
                }
            }
            
            _itemsLoaded = true;
        }
    }
}
