using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using JsonType;
using Newtonsoft.Json;
using WTTClientCommonLib.Converters;

namespace WTTClientCommonLib.Models;

public class ExtendedProductionScheme : BaseHideoutScheme
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

                var items = Singleton<ItemFactory>
                    .Instance.FlatItemsToTree(craftResult.Items)
                    .Items;

                foreach ((string _, Item item) in items)
                {
                    if (recipeStackBaseItems.TryGetValue(resultId, out string _))
                    {
                        bool isAdded = ResultItemStacks.TryGetValue(resultId, out _);

                        if (!isAdded)
                        {
                            RecipeResultStack resultStack;

                            if (
                                craftResult.MinStackCount.HasValue
                                && craftResult.MaxStackCount.HasValue
                            )
                            {
                                resultStack = new RecipeResultStack
                                {
                                    Item = item,
                                    Count = craftResult.Count,
                                    MinStackCount = craftResult.MinStackCount.Value,
                                    MaxStackCount = craftResult.MaxStackCount.Value,
                                };
                            }
                            else
                            {
                                resultStack = new RecipeResultStack
                                {
                                    Item = item,
                                    Count = craftResult.Count,
                                };
                            }

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
    public int? MinStackCount;
    public int? MaxStackCount;
}

public class CustomCraftResult
{
    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("minStackCount")]
    public int? MinStackCount { get; set; }

    [JsonProperty("maxStackCount")]
    public int? MaxStackCount { get; set; }

    [JsonProperty("items")]
    public FlatItem[] Items { get; set; }
}
