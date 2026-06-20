using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;

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

    public override void Validate(string itemId)
    {
        base.Validate(itemId);

        if (AddToTraders == true)
        {
            if (Traders == null || Traders.Count == 0)
                throw new InvalidDataException($"[{itemId}] traders is required when addtoTraders is true");

            foreach (var traderEntry in Traders)
            {
                var traderKey = traderEntry.Key;
                var schemes = traderEntry.Value;

                if (string.IsNullOrWhiteSpace(traderKey))
                    throw new InvalidDataException($"[{itemId}] traders contains an empty trader key");

                if (schemes == null || schemes.Count == 0)
                    throw new InvalidDataException($"[{itemId}] traders['{traderKey}'] must contain at least one scheme");

                foreach (var schemeEntry in schemes)
                {
                    var schemeKey = schemeEntry.Key;
                    var scheme = schemeEntry.Value;

                    if (string.IsNullOrWhiteSpace(schemeKey))
                        throw new InvalidDataException($"[{itemId}] traders['{traderKey}'] contains an empty scheme key");

                    if (scheme == null)
                        throw new InvalidDataException($"[{itemId}] traders['{traderKey}']['{schemeKey}'] is null");

                    if (scheme.ConfigBarterSettings == null)
                        throw new InvalidDataException($"[{itemId}] traders['{traderKey}']['{schemeKey}'].barterSettings is required");

                    if (scheme.ConfigBarterSettings.LoyalLevel < 0)
                        throw new InvalidDataException($"[{itemId}] traders['{traderKey}']['{schemeKey}'].barterSettings.loyalLevel must be >= 0");

                    if (scheme.ConfigBarterSettings.StackObjectsCount < 0)
                        throw new InvalidDataException($"[{itemId}] traders['{traderKey}']['{schemeKey}'].barterSettings.stackObjectsCount must be >= 0");

                    if (scheme.Barters == null || scheme.Barters.Count == 0)
                        throw new InvalidDataException($"[{itemId}] traders['{traderKey}']['{schemeKey}'] must include at least one barter");

                    for (var i = 0; i < scheme.Barters.Count; i++)
                    {
                        var barter = scheme.Barters[i];

                        if (barter == null)
                            throw new InvalidDataException($"[{itemId}] traders['{traderKey}']['{schemeKey}'].barters[{i}] is null");

                        if (string.IsNullOrWhiteSpace(barter.Template))
                            throw new InvalidDataException($"[{itemId}] traders['{traderKey}']['{schemeKey}'].barters[{i}].template is required");

                        if (barter.Count <= 0)
                            throw new InvalidDataException($"[{itemId}] traders['{traderKey}']['{schemeKey}'].barters[{i}].count must be > 0");
                    }
                }
            }
        }

        if (AddToStaticLootContainers == true)
        {
            if (StaticLootContainers == null || StaticLootContainers.Count == 0)
                throw new InvalidDataException($"[{itemId}] staticLootContainers is required when addtoStaticLootContainers is true");

            for (var i = 0; i < StaticLootContainers.Count; i++)
            {
                var c = StaticLootContainers[i];

                if (c == null)
                    throw new InvalidDataException($"[{itemId}] staticLootContainers[{i}] is null");

                if (string.IsNullOrWhiteSpace(c.ContainerName))
                    throw new InvalidDataException($"[{itemId}] staticLootContainers[{i}].containerName is required");

                if (c.Probability < 0)
                    throw new InvalidDataException($"[{itemId}] staticLootContainers[{i}].probability must be >= 0");
            }
        }

        if (Masteries == true)
        {
            if (MasterySections == null || MasterySections.Count == 0)
                throw new InvalidDataException($"[{itemId}] masterySections is required when masteries is true");

            for (var i = 0; i < MasterySections.Count; i++)
            {
                var m = MasterySections[i];

                if (m == null)
                    throw new InvalidDataException($"[{itemId}] masterySections[{i}] is null");

                if (m.Templates == null)
                    throw new InvalidDataException($"[{itemId}] masterySections[{i}].templates is required");

                if (m.Name == null)
                    throw new InvalidDataException($"[{itemId}] masterySections[{i}].name is required");

                if (m.Level2 < 0)
                    throw new InvalidDataException($"[{itemId}] masterySections[{i}].level2 must be >= 0");

                if (m.Level3 < 0)
                    throw new InvalidDataException($"[{itemId}] masterySections[{i}].level3 must be >= 0");
            }
        }

        if (AddWeaponPreset == true)
        {
            if (WeaponPresets == null || WeaponPresets.Count == 0)
                throw new InvalidDataException($"[{itemId}] weaponPresets is required when addWeaponPreset is true");

            for (var i = 0; i < WeaponPresets.Count; i++)
            {
                var p = WeaponPresets[i];

                if (p == null)
                    throw new InvalidDataException($"[{itemId}] weaponPresets[{i}] is null");

                if (string.IsNullOrWhiteSpace(p.Id.ToString()))
                    throw new InvalidDataException($"[{itemId}] weaponPresets[{i}]._id is required");

                if (string.IsNullOrWhiteSpace(p.Type))
                    throw new InvalidDataException($"[{itemId}] weaponPresets[{i}]._type is required");

                if (string.IsNullOrWhiteSpace(p.Name))
                    throw new InvalidDataException($"[{itemId}] weaponPresets[{i}]._name is required");

                if (string.IsNullOrWhiteSpace(p.Parent.ToString()))
                    throw new InvalidDataException($"[{itemId}] weaponPresets[{i}]._parent is required");

                if (p.Items == null || p.Items.Count == 0)
                    throw new InvalidDataException($"[{itemId}] weaponPresets[{i}] must include at least one item");

                for (var j = 0; j < p.Items.Count; j++)
                {
                    var item = p.Items[j];

                    if (item == null)
                        throw new InvalidDataException($"[{itemId}] weaponPresets[{i}].items[{j}] is null");

                    if (item.Id == null || string.IsNullOrWhiteSpace(item.Id.ToString()))
                        throw new InvalidDataException($"[{itemId}] weaponPresets[{i}].items[{j}]._id is required");

                    if (item.Template == null || string.IsNullOrWhiteSpace(item.Template.ToString()))
                        throw new InvalidDataException($"[{itemId}] weaponPresets[{i}].items[{j}]._tpl is required");

                    if (!string.IsNullOrWhiteSpace(item.ParentId) && string.IsNullOrWhiteSpace(item.SlotId))
                        throw new InvalidDataException($"[{itemId}] weaponPresets[{i}].items[{j}] has parentId but no slotId");

                    if (!string.IsNullOrWhiteSpace(item.SlotId) && string.IsNullOrWhiteSpace(item.ParentId))
                        throw new InvalidDataException($"[{itemId}] weaponPresets[{i}].items[{j}] has slotId but no parentId");
                }
            }
        }

        if (AddToHallOfFame == true)
        {
            if (HallOfFameSlots == null || HallOfFameSlots.Count == 0)
                throw new InvalidDataException($"[{itemId}] hallOfFameSlots is required when addtoHallOfFame is true");

            for (var i = 0; i < HallOfFameSlots.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(HallOfFameSlots[i]))
                    throw new InvalidDataException($"[{itemId}] hallOfFameSlots[{i}] must be a non-empty string");
            }
        }

        if (AddToStaticAmmo == true)
        {
            if (StaticAmmoProbability == null)
                throw new InvalidDataException($"[{itemId}] staticAmmoProbability is required when addtoStaticAmmo is true");

            if (StaticAmmoProbability < 0)
                throw new InvalidDataException($"[{itemId}] staticAmmoProbability must be >= 0");
        }

        if (AddToEmptyPropSlots == true && EmptyPropSlot == null)
            throw new InvalidDataException($"[{itemId}] emptyPropSlot is required when addtoEmptyPropSlots is true");

        if (ParentId == "62f109593b54472778797866")
        {
            if (IsRandomLootContainer != true)
            {
                throw new InvalidDataException(
                    $"[{itemId}] isRandomLootContainer must be true when parentId is RandomLootContainer");
            }

            if (RandomLootContainerRewards == null)
            {
                throw new InvalidDataException(
                    $"[{itemId}] randomLootContainerRewards is required when parentId is RandomLootContainer");
            }
        }
    }
}

public class ConfigTraderScheme
{
    [JsonPropertyName("barterSettings")] public required ConfigBarterSettings ConfigBarterSettings { get; set; }

    [JsonPropertyName("barters")] public required List<ConfigBarterScheme> Barters { get; set; } = new();
    
}

public class ConfigBarterSettings
{
    [JsonPropertyName("loyalLevel")] public required int LoyalLevel { get; set; }

    [JsonPropertyName("unlimitedCount")] public required bool UnlimitedCount { get; set; }

    [JsonPropertyName("stackObjectsCount")] public required int StackObjectsCount { get; set; }

    [JsonPropertyName("buyRestrictionMax")] public int? BuyRestrictionMax { get; set; }
    
}

public class ConfigBarterScheme
{
    [JsonPropertyName("count")] public virtual double? Count { get; set; }

    [JsonPropertyName("_tpl")] public virtual string Template { get; set; }

    [JsonPropertyName("onlyFunctional")] public virtual bool? OnlyFunctional { get; set; }

    [JsonPropertyName("sptQuestLocked")] public virtual bool? SptQuestLocked { get; set; }

    [JsonPropertyName("level")] public virtual int? Level { get; set; }

    [JsonPropertyName("side")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public virtual DogtagExchangeSide? Side { get; set; }
}

public class ConfigStaticLootContainer
{
    [JsonPropertyName("containerName")] public required string ContainerName { get; set; } = string.Empty;

    [JsonPropertyName("probability")] public required int Probability { get; set; }
}

public class EmptySlotScheme
{
    [JsonPropertyName("itemToAddTo")]
    public string ItemToAddTo { get; set; } = string.Empty;
        
    [JsonPropertyName("modSlot")]
    public string ModSlot { get; set; } = string.Empty;
}