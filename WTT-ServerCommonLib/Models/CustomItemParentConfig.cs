using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace WTTServerCommonLib.Models;

public record CustomItemParentConfig : TemplateItem
{
    [JsonPropertyName("addToContainerFilters")]
    public bool AddToContainers { get; set; } = false;

    [JsonPropertyName("containers")]
    public List<string> Containers { get; set; } = [];

    [JsonPropertyName("addToInventorySlots")]
    public bool AddToInventorySlots { get; set; } = false;

    [JsonPropertyName("inventorySlots")]
    public List<string> InventorySlots { get; set; } = [];

    [JsonPropertyName("addToTraderBuyLists")]
    public bool AddToTraderBuyLists { get; set; } = false;

    [JsonPropertyName("traders")]
    public List<string> Traders { get; set; } = [];
}
