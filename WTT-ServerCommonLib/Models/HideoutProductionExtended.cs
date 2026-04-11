using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using System.Text.Json.Serialization;

namespace WTTServerCommonLib.Models;

public record HideoutProductionExtended : HideoutProduction
{
    [JsonPropertyName("endProductItems")]
    public Dictionary<MongoId, CustomCraftResult>? EndProductItems { get; set; }

    public List<List<Item>> GetResultItems()
    {
        if (EndProductItems == null) return [];
        
        List<CustomCraftResult> craftResults = EndProductItems.Values.ToList();
        List<List<Item>> finalItems = [];
        
        if (craftResults.Count == 0)
        {
            return [];
        }

        foreach (var result in craftResults)
        {
            int count = result.Count;

            for (int i = 0; i < count; i++)
            {
                finalItems.Add(result.Items);
            }
        }

        return finalItems;
    }
}

public class CustomCraftResult
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
    
    [JsonPropertyName("items")]
    public List<Item> Items { get; set; }
}
