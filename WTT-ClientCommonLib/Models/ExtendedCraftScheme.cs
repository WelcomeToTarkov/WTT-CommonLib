using Comfort.Common;
using EFT.Hideout;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;
using WTTClientCommonLib.Converters;

namespace WTTClientCommonLib.Models;

public class ExtendedProductionScheme : ProductionBuildAbstractClass
{
    private bool _itemsLoaded = false;
    
    [JsonProperty("_id")]
    public string _id { get; set; }
    
    [JsonProperty("endProductItems")]
    public FlatItemsDataClass[] EndProductItems { get; set; }

    [JsonProperty("requirements")]
    [JsonConverter(typeof(RequirementArrayConverter))]
    public new Requirement[] requirements { get; set; }
    
    public List<Item> ResultItems = [];
    public List<Item> BaseItems = [];

    public void LoadResultItems()
    {
        if (!_itemsLoaded)
        {
            List<string> baseItemTemplateIds = [];
            foreach (var flatItem in EndProductItems)
            {
                if (string.IsNullOrEmpty(flatItem.slotId))
                {
                    baseItemTemplateIds.Add(flatItem._tpl);
                }
            }
            
            var items = Singleton<ItemFactoryClass>.Instance.FlatItemsToTree(EndProductItems).Items;
            foreach ((string id, Item item) in items)
            {
                ResultItems.Add(item);

                if (baseItemTemplateIds.Contains(item.StringTemplateId))
                {
                    BaseItems.Add(item);
                }
            }
            
            _itemsLoaded = true;
        }
    }
}
