using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Text.Json.Serialization;

namespace WTTServerCommonLib.Models;

public record CustomItemParentConfig : TemplateItem
{
    [JsonPropertyName("addToContainerFilters")]
    public bool AddToContainers { get; set; } = false;
    
    [JsonPropertyName("containers")]
    public List<MongoId> Containers { get; set; } = [];
}
