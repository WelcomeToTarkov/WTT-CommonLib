using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WTTServerCommonLib.Models;

[UsedImplicitly]
public record HideoutProductionExtended : HideoutProduction
{
    [JsonPropertyName("endProductItems")]
    public Dictionary<MongoId, CustomCraftResult>? EndProductItems { get; set; }

    private readonly Random _rand = new();

    public List<List<Item>> GetResultItems()
    {
        if (EndProductItems == null) return [];
        
        var craftResults = EndProductItems.Values.ToList();
        List<List<Item>> finalItems = [];
        
        if (craftResults.Count == 0)
        {
            return [];
        }

        foreach (var result in craftResults)
        {
            var count = result.Count;
            
            if (result is { MinCount: not null, MaxCount: not null })
            {
                count = _rand.Next((int)result.MinCount, (int)result.MaxCount);
            }
            
            for (var i = 0; i < count; i++)
            {
                finalItems.Add(result.Items);
            }
        }

        return finalItems;
    }
}

[UsedImplicitly]
public class CustomCraftResult
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
    
    [JsonPropertyName("minCount")]
    public int? MinCount { get; set; }
    
    [JsonPropertyName("maxCount")]
    public int? MaxCount { get; set; }
    
    [JsonPropertyName("items")]
    public List<Item> Items { get; set; }
}
