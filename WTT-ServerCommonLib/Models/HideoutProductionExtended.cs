using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using System.Text.Json.Serialization;

namespace WTTServerCommonLib.Models;

public record HideoutProductionExtended : HideoutProduction
{
    [JsonPropertyName("endProductItems")]
    public List<Item>? EndProductItems { get; set; }
}
