using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace WTTServerCommonLib.Models;

public class CustomItemConfig : CustomItemConfigBase
{
    [JsonPropertyName("addtoTraders")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddToTraders { get; set; }

    [JsonPropertyName("traders")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, Dictionary<MongoId, ConfigTraderScheme>>? Traders { get; set; }

    [JsonPropertyName("addtoBots")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddToBots { get; set; }

    [JsonPropertyName("addtoStaticLootContainers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddToStaticLootContainers { get; set; }

    [JsonPropertyName("staticLootContainers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ConfigStaticLootContainer>? StaticLootContainers { get; set; }

    [JsonPropertyName("masteries")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Masteries { get; set; }

    [JsonPropertyName("masterySections")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Mastering>? MasterySections { get; set; }

    [JsonPropertyName("addWeaponPreset")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddWeaponPreset { get; set; }

    [JsonPropertyName("weaponPresets")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Preset>? WeaponPresets { get; set; }

    [JsonPropertyName("addtoHallOfFame")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddToHallOfFame { get; set; }

    [JsonPropertyName("hallOfFameSlots")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? HallOfFameSlots { get; set; }

    [JsonPropertyName("addtoGeneratorAsFuel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddToGeneratorAsFuel { get; set; }

    [JsonPropertyName("generatorFuelSlotStages")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? GeneratorFuelSlotStages { get; set; }

    [JsonPropertyName("addtoHideoutPosterSlots")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddToHideoutPosterSlots { get; set; }

    [JsonPropertyName("addPosterToMaps")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddPosterToMaps { get; set; }

    [JsonPropertyName("posterSpawnProbability")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PosterSpawnProbability { get; set; }

    [JsonPropertyName("addtoStatuetteSlots")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddToStatuetteSlots { get; set; }

    [JsonPropertyName("addCaliberToAllCloneLocations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddCaliberToAllCloneLocations { get; set; }

    [JsonPropertyName("addtoStaticAmmo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddToStaticAmmo { get; set; }

    [JsonPropertyName("staticAmmoProbability")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StaticAmmoProbability { get; set; }

    [JsonPropertyName("addtoEmptyPropSlots")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AddToEmptyPropSlots { get; set; }

    [JsonPropertyName("emptyPropSlot")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EmptySlotScheme? EmptyPropSlot { get; set; }

    [JsonPropertyName("isRandomLootContainer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRandomLootContainer { get; set; }

    [JsonPropertyName("randomLootContainerRewards")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RewardDetails? RandomLootContainerRewards { get; set; }

    public override IEnumerable<string> GetValidationErrors(string itemId)
    {
        foreach (var error in base.GetValidationErrors(itemId))
            yield return error;

        if (AddToTraders == true)
        {
            if (Traders == null || Traders.Count == 0)
            {
                yield return $"traders is required when addtoTraders is true";
            }
            else
            {
                foreach (var traderEntry in Traders)
                {
                    var traderKey = traderEntry.Key;
                    var schemes = traderEntry.Value;

                    if (string.IsNullOrWhiteSpace(traderKey))
                    {
                        yield return $"traders contains an empty trader key";
                        continue;
                    }

                    if (schemes == null || schemes.Count == 0)
                    {
                        yield return $"traders['{traderKey}'] must contain at least one scheme";
                        continue;
                    }

                    foreach (var schemeEntry in schemes)
                    {
                        var schemeKey = schemeEntry.Key;
                        var scheme = schemeEntry.Value;

                        if (string.IsNullOrWhiteSpace(schemeKey.ToString()))
                        {
                            yield return $"traders['{traderKey}'] contains an empty scheme key";
                            continue;
                        }

                        if (scheme == null)
                        {
                            yield return $"traders['{traderKey}']['{schemeKey}'] is null";
                            continue;
                        }

                        if (scheme.ConfigBarterSettings == null)
                        {
                            yield return $"traders['{traderKey}']['{schemeKey}'].barterSettings is required";
                        }
                        else
                        {
                            if (scheme.ConfigBarterSettings.LoyalLevel < 0)
                                yield return $"traders['{traderKey}']['{schemeKey}'].barterSettings.loyalLevel must be >= 0";

                            if (scheme.ConfigBarterSettings.StackObjectsCount < 0)
                                yield return $"traders['{traderKey}']['{schemeKey}'].barterSettings.stackObjectsCount must be >= 0";
                        }

                        if (scheme.Barters == null || scheme.Barters.Count == 0)
                        {
                            yield return $"traders['{traderKey}']['{schemeKey}'] must include at least one barter";
                            continue;
                        }

                        for (var i = 0; i < scheme.Barters.Count; i++)
                        {
                            var barter = scheme.Barters[i];

                            if (barter == null)
                            {
                                yield return $"traders['{traderKey}']['{schemeKey}'].barters[{i}] is null";
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(barter.Template))
                                yield return $"traders['{traderKey}']['{schemeKey}'].barters[{i}].template is required";

                            if (barter.Count == null || barter.Count <= 0)
                                yield return $"traders['{traderKey}']['{schemeKey}'].barters[{i}].count must be > 0";
                        }
                    }
                }
            }
        }

        if (AddToStaticLootContainers == true)
        {
            if (StaticLootContainers == null || StaticLootContainers.Count == 0)
            {
                yield return $"staticLootContainers is required when addtoStaticLootContainers is true";
            }
            else
            {
                for (var i = 0; i < StaticLootContainers.Count; i++)
                {
                    var c = StaticLootContainers[i];

                    if (c == null)
                    {
                        yield return $"staticLootContainers[{i}] is null";
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(c.ContainerName))
                        yield return $"staticLootContainers[{i}].containerName is required";

                    if (c.Probability < 0)
                        yield return $"staticLootContainers[{i}].probability must be >= 0";
                }
            }
        }

        if (Masteries == true)
        {
            if (MasterySections == null || MasterySections.Count == 0)
            {
                yield return $"masterySections is required when masteries is true";
            }
            else
            {
                for (var i = 0; i < MasterySections.Count; i++)
                {
                    var m = MasterySections[i];

                    if (m == null)
                    {
                        yield return $"masterySections[{i}] is null";
                        continue;
                    }

                    if (m.Templates == null)
                        yield return $"masterySections[{i}].templates is required";

                    if (m.Name == null)
                        yield return $"masterySections[{i}].name is required";

                    if (m.Level2 < 0)
                        yield return $"masterySections[{i}].level2 must be >= 0";

                    if (m.Level3 < 0)
                        yield return $"masterySections[{i}].level3 must be >= 0";
                }
            }
        }

        if (AddWeaponPreset == true)
        {
            if (WeaponPresets == null || WeaponPresets.Count == 0)
            {
                yield return $"weaponPresets is required when addWeaponPreset is true";
            }
            else
            {
                for (var i = 0; i < WeaponPresets.Count; i++)
                {
                    var p = WeaponPresets[i];

                    if (p == null)
                    {
                        yield return $"weaponPresets[{i}] is null";
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(p.Id.ToString()))
                        yield return $"weaponPresets[{i}]._id is required";

                    if (string.IsNullOrWhiteSpace(p.Type))
                        yield return $"weaponPresets[{i}]._type is required";

                    if (string.IsNullOrWhiteSpace(p.Name))
                        yield return $"weaponPresets[{i}]._name is required";

                    if (string.IsNullOrWhiteSpace(p.Parent.ToString()))
                        yield return $"weaponPresets[{i}]._parent is required";

                    if (p.Items == null || p.Items.Count == 0)
                    {
                        yield return $"weaponPresets[{i}] must include at least one item";
                        continue;
                    }

                    for (var j = 0; j < p.Items.Count; j++)
                    {
                        var item = p.Items[j];

                        if (item == null)
                        {
                            yield return $"weaponPresets[{i}].items[{j}] is null";
                            continue;
                        }

                        if (item.Id == null || string.IsNullOrWhiteSpace(item.Id.ToString()))
                            yield return $"weaponPresets[{i}].items[{j}]._id is required";

                        if (
                            item.Template == null
                            || string.IsNullOrWhiteSpace(item.Template.ToString())
                        )
                            yield return $"weaponPresets[{i}].items[{j}]._tpl is required";

                        if (
                            !string.IsNullOrWhiteSpace(item.ParentId)
                            && string.IsNullOrWhiteSpace(item.SlotId)
                        )
                            yield return $"weaponPresets[{i}].items[{j}] has parentId but no slotId";

                        if (
                            !string.IsNullOrWhiteSpace(item.SlotId)
                            && string.IsNullOrWhiteSpace(item.ParentId)
                        )
                            yield return $"weaponPresets[{i}].items[{j}] has slotId but no parentId";
                    }
                }
            }
        }

        if (AddToHallOfFame == true)
        {
            if (HallOfFameSlots == null || HallOfFameSlots.Count == 0)
            {
                yield return $"hallOfFameSlots is required when addtoHallOfFame is true";
            }
            else
            {
                for (var i = 0; i < HallOfFameSlots.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(HallOfFameSlots[i]))
                        yield return $"hallOfFameSlots[{i}] must be a non-empty string";
                }
            }
        }

        if (AddToStaticAmmo == true)
        {
            if (StaticAmmoProbability == null)
                yield return $"staticAmmoProbability is required when addtoStaticAmmo is true";

            if (StaticAmmoProbability < 0)
                yield return $"staticAmmoProbability must be >= 0";
        }

        if (AddToEmptyPropSlots == true && EmptyPropSlot == null)
            yield return $"emptyPropSlot is required when addtoEmptyPropSlots is true";

        if (ParentId == "62f109593b54472778797866")
        {
            if (IsRandomLootContainer != true)
                yield return $"isRandomLootContainer must be true when parentId is RandomLootContainer";

            if (RandomLootContainerRewards == null)
                yield return $"randomLootContainerRewards is required when parentId is RandomLootContainer";
        }
    }
}

public class ConfigTraderScheme
{
    [JsonPropertyName("barterSettings")]
    public required ConfigBarterSettings ConfigBarterSettings { get; set; }

    [JsonPropertyName("barters")]
    public required List<ConfigBarterScheme> Barters { get; set; } = new();
}

public class ConfigBarterSettings
{
    [JsonPropertyName("loyalLevel")]
    public required int LoyalLevel { get; set; }

    [JsonPropertyName("unlimitedCount")]
    public required bool UnlimitedCount { get; set; }

    [JsonPropertyName("stackObjectsCount")]
    public required int StackObjectsCount { get; set; }

    [JsonPropertyName("buyRestrictionMax")]
    public int? BuyRestrictionMax { get; set; }
}

public class ConfigBarterScheme
{
    [JsonPropertyName("count")]
    public virtual double? Count { get; set; }

    [JsonPropertyName("_tpl")]
    public virtual string Template { get; set; }

    [JsonPropertyName("onlyFunctional")]
    public virtual bool? OnlyFunctional { get; set; }

    [JsonPropertyName("sptQuestLocked")]
    public virtual bool? SptQuestLocked { get; set; }

    [JsonPropertyName("level")]
    public virtual int? Level { get; set; }

    [JsonPropertyName("side")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public virtual DogtagExchangeSide? Side { get; set; }
}

public class ConfigStaticLootContainer
{
    [JsonPropertyName("containerName")]
    public required string ContainerName { get; set; } = string.Empty;

    [JsonPropertyName("probability")]
    public required int Probability { get; set; }
}

public class EmptySlotScheme
{
    [JsonPropertyName("itemToAddTo")]
    public string ItemToAddTo { get; set; } = string.Empty;

    [JsonPropertyName("modSlot")]
    public string ModSlot { get; set; } = string.Empty;
}
