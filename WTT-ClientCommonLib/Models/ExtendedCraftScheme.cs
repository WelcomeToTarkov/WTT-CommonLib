using Comfort.Common;
using EFT.Hideout;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security;
using WTTClientCommonLib.Converters;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Models;

public class ExtendedProductionScheme : ProductionBuildAbstractClass
{
    private bool _itemsLoaded = false;
    
    [JsonProperty("_id")]
    public string _id { get; set; }
    
    [JsonProperty("endProductItems")]
    public Dictionary<string, CustomCraftResult> EndProductItems { get; set; }

    [JsonProperty("requirements")]
    [JsonConverter(typeof(RequirementArrayConverter))]
    public new Requirement[] requirements { get; set; }
    
    public Dictionary<string, RecipeResultStack> ResultItemStacks = [];
    public RecipeResultStack FirstResult;

    public void LoadResultItems()
    {
        if (!_itemsLoaded)
        {
            Dictionary<string, string> recipeStackBaseItems = [];
            
            foreach ((string resultId, CustomCraftResult craftResult) in EndProductItems)
            {
                foreach (var item in craftResult.Items)
                {
                    if (string.IsNullOrEmpty(item.slotId))
                    {
                        recipeStackBaseItems.Add(resultId, item._tpl);
                        break;
                    }
                }
                
                var items = Singleton<ItemFactoryClass>.Instance.FlatItemsToTree(craftResult.Items).Items;
                foreach ((string id, Item item) in items)
                {
                    if (recipeStackBaseItems.TryGetValue(resultId, out string baseItemTpl))
                    {
                        bool isAdded = ResultItemStacks.TryGetValue(resultId, out _);

                        if (!isAdded)
                        {
                            RecipeResultStack resultStack = new RecipeResultStack
                            {
                                Item = item,
                                Count = craftResult.Count
                            };
                            
                            ResultItemStacks.Add(resultId, resultStack);

                            if (FirstResult == null)
                            {
                                FirstResult = resultStack;
                            }

                            break;
                        }
                    }
                }
            }
            
            _itemsLoaded = true;
        }
    }
}

public class RecipeResultStack
{
    public Item Item;
    public int Count;
}

public class CustomCraftResult
{
    [JsonProperty("count")]
    public int Count { get; set; }
    
    [JsonProperty("items")]
    public FlatItemsDataClass[] Items { get; set; }
}
