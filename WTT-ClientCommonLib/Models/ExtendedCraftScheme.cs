using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace WTTClientCommonLib.Models;

public class ExtendedProductionScheme
{
    private bool _itemsLoaded = false;
    
    [JsonProperty("_id")]
    public string _id { get; set; }
    
    [JsonProperty("endProductItems")]
    public FlatItemsDataClass[] EndProductItems { get; set; }
    
    [JsonProperty("areaType")]
    public EAreaType AreaType { get; set; }

    // no clue how to get this to work, unnecessary anyway for current purposes
    /*
    [JsonProperty("requirements")]
    public List<Requirement> Requirements { get; set; }
    */

    [JsonProperty("productionTime")]
    public int ProductionTime { get; set; }
    
    [JsonProperty("endProduct")]
    public string EndProduct { get; set; }
    
    [JsonProperty("isEncoded")]
    public bool IsEncoded { get; set; }
    
    [JsonProperty("locked")]
    public bool Locked { get; set; }
    
    [JsonProperty("needFuelForAllProductionTime")]
    public bool NeedFuelForAllProductionTime { get; set; }
    
    [JsonProperty("continuous")]
    public bool Continuous { get; set; }
    
    [JsonProperty("count")]
    public int Count { get; set; }
    
    [JsonProperty("productionLimitCount")]
    public int ProductionLimitCount { get; set; }
    
    public List<Item> ResultItems = [];
    public List<Item> BaseItems = [];

    public void LoadResultItems()
    {
        if (!_itemsLoaded)
        {
            List<string> baseItemTpls = [];
            foreach (var flatItem in EndProductItems)
            {
                if (flatItem.slotId != string.Empty)
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
