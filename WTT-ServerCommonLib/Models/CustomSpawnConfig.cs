using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common;

namespace WTTServerCommonLib.Models;

public class CustomSpawnConfig
{
    [JsonPropertyName("questId")]
    public string QuestId { get; set; }

    [JsonPropertyName("locationID")]
    public string LocationID { get; set; }

    [JsonPropertyName("bundleName")]
    public string BundleName { get; set; }

    [JsonPropertyName("prefabName")]
    public string PrefabName { get; set; }

    [JsonPropertyName("position")]
    public Vector3 Position { get; set; }

    [JsonPropertyName("rotation")]
    public Vector3 Rotation { get; set; }

    [JsonPropertyName("requiredQuestStatuses")]
    public List<string> RequiredQuestStatuses { get; set; } = new();

    [JsonPropertyName("excludedQuestStatuses")]
    public List<string> ExcludedQuestStatuses { get; set; } = new();

    [JsonPropertyName("questMustExist")]
    public bool? QuestMustExist { get; set; }

    [JsonPropertyName("linkedQuestId")]
    public string LinkedQuestId { get; set; }

    [JsonPropertyName("linkedRequiredStatuses")]
    public List<string> LinkedRequiredStatuses { get; set; } = new();

    [JsonPropertyName("linkedExcludedStatuses")]
    public List<string> LinkedExcludedStatuses { get; set; } = new();

    [JsonPropertyName("linkedQuestMustExist")]
    public bool? LinkedQuestMustExist { get; set; }

    [JsonPropertyName("requiredItemInInventory")]
    public string RequiredItemInInventory { get; set; }

    [JsonPropertyName("requiredLevel")]
    public int? RequiredLevel { get; set; }

    [JsonPropertyName("requiredFaction")]
    public string RequiredFaction { get; set; }

    [JsonPropertyName("requiredBossSpawned")]
    public string RequiredBossSpawned { get; set; }
}
