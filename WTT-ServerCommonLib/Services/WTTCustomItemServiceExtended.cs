using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;
using WTTServerCommonLib.Constants;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using WTTServerCommonLib.Services.ItemServiceHelpers;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomItemServiceExtended(
    ISptLogger<WTTCustomItemServiceExtended> logger,
    TemplateTable templateTable,
    ModHelper modHelper,
    WeaponPresetHelper weaponPresetHelper,
    TraderItemHelper traderItemHelper,
    StaticLootHelper staticLootHelper,
    SpecialSlotsHelper specialSlotsHelper,
    PosterLootHelper posterLootHelper,
    ModSlotHelper modSlotHelper,
    MasteryHelper masteryHelper,
    InventorySlotHelper inventorySlotHelper,
    HideoutStatuetteHelper hideoutStatuetteHelper,
    HideoutPosterHelper hideoutPosterHelper,
    HallOfFameHelper hallOfFameHelper,
    GeneratorFuelHelper generatorFuelHelper,
    CaliberHelper caliberHelper,
    BotLootHelper botLootHelper,
    ConfigHelper configHelper,
    StaticAmmoHelper staticAmmoHelper,
    EmptyPropSlotHelper emptyPropSlotHelper,
    SecureFiltersHelper secureFiltersHelper,
    RandomLootContainerHelper randomLootContainerHelper,
    ICloner cloner,
    ItemHelper itemHelper,
    ItemRegistrationHelper itemRegistrationHelper,
    ItemBlacklistHelper rewardItemBlacklistHelper
)
{
    private readonly List<(string newItemId, CustomItemConfig config)> _deferredModSlotConfigs =
        new();
    private readonly List<(
        string newItemId,
        CustomItemConfig config
    )> _deferredSecureFilterConfigs = new();
    private readonly List<(string newItemId, CustomItemConfig config)> _deferredCaliberConfigs =
        new();
    private CommonlibConfig _commonlibConfig;

    /// <summary>
    /// Loads custom item configurations from JSON/JSONC files and creates items with all associated properties.
    ///
    /// Items are loaded from the mod's "db/CustomItems" directory (or a custom path if specified).
    /// Each item is cloned from a base template and can be configured with traders, presets, masteries, slots, loot tables, and more.
    ///
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomItems(Assembly assembly, string? relativePath = null)
    {
        var file = Path.Combine(
            modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()),
            "config.jsonc"
        );
        var commonlibConfig = await configHelper.LoadJsonFile<CommonlibConfig>(file);
        if (commonlibConfig != null)
        {
            _commonlibConfig = commonlibConfig;
        }

        try
        {
            var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            var modName = assembly.GetName().Name ?? "UnknownMod";
            var defaultDir = Path.Combine("db", "CustomItems");
            var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

            if (!Directory.Exists(finalDir))
            {
                logger.Error($"Directory not found at {finalDir}");
                return;
            }

            var itemConfigDicts = await configHelper.LoadAllJsonFiles<
                Dictionary<string, CustomItemConfig>
            >(finalDir);

            if (itemConfigDicts.Count == 0)
            {
                logger.Warning($"No valid item configs found in {finalDir}");
                return;
            }

            var totalItemsCreated = 0;
            var validationFailures = new List<(string ItemId, List<string> Errors)>();
            var creationFailures = new List<(string ItemId, string Error)>();

            foreach (var configDict in itemConfigDicts)
            {
                foreach (var (itemId, configData) in configDict)
                {
                    var validationErrors = configData.GetValidationErrors(itemId).ToList();

                    if (validationErrors.Count > 0)
                    {
                        validationFailures.Add((itemId, validationErrors));
                    }

                    try
                    {
                        CreateItemFromConfig(assembly, itemId, configData);
                        totalItemsCreated++;
                    }
                    catch (Exception ex)
                    {
                        creationFailures.Add((itemId, ex.Message));
                    }
                }
            }

            LogHelper.Debug(
                logger,
                $"Created {totalItemsCreated} custom items from {itemConfigDicts.Count} files"
            );

            if (validationFailures.Count > 0)
            {
                var warningSb = new System.Text.StringBuilder();

                warningSb.AppendLine();
                warningSb.AppendLine("==================================================");
                warningSb.AppendLine($"CUSTOM ITEM VALIDATION WARNINGS FOR MOD: {modName}");
                warningSb.AppendLine("==================================================");
                warningSb.AppendLine($"Created items: {totalItemsCreated}");
                warningSb.AppendLine($"Validation failures: {validationFailures.Count}");
                warningSb.AppendLine("==================================================");
                warningSb.AppendLine("VALIDATION FAILURES:");
                warningSb.AppendLine("--------------------------------------------------");

                foreach (var failure in validationFailures)
                {
                    warningSb.AppendLine($"[{failure.ItemId}]");

                    foreach (var error in failure.Errors)
                        warningSb.AppendLine($" - {error}");

                    warningSb.AppendLine();
                }

                warningSb.AppendLine("==================================================");
                if (_commonlibConfig.ItemValidationLoggingEnabled)
                {
                    LogHelper.WriteWarning(warningSb.ToString());
                }
            }

            if (creationFailures.Count > 0)
            {
                var errorSb = new System.Text.StringBuilder();

                errorSb.AppendLine();
                errorSb.AppendLine("==================================================");
                errorSb.AppendLine($"CUSTOM ITEM CREATION ERRORS FOR MOD: {modName}");
                errorSb.AppendLine("==================================================");
                errorSb.AppendLine($"Created items: {totalItemsCreated}");
                errorSb.AppendLine($"Creation failures: {creationFailures.Count}");
                errorSb.AppendLine("==================================================");
                errorSb.AppendLine("CREATION FAILURES:");
                errorSb.AppendLine("--------------------------------------------------");

                foreach (var failure in creationFailures)
                    errorSb.AppendLine($"[{failure.ItemId}] {failure.Error}");

                errorSb.AppendLine("==================================================");


                LogHelper.WriteError(errorSb.ToString());
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading configs: {ex.Message}");
        }
    }

    private bool CreateItemFromConfig(Assembly assembly, string newItemId, CustomItemConfig config)
    {
        var modName = assembly.GetName().Name ?? "UnknownMod";

        CreateItemFromClone(assembly, newItemId, config);
        LogHelper.Debug(logger, $"{modName}:Created item {newItemId}");

        ProcessAdditionalProperties(newItemId, config);
        return true;
    }

    private void CreateItemFromClone(Assembly assembly, string newItemId, CustomItemConfig config)
    {
        if (templateTable.Items.TryGetValue(newItemId, out var existingItem))
            throw new InvalidOperationException($"ItemId already exists. {existingItem.Name}");

        var cloneTplId = ItemTplResolver.ResolveId(config.ItemTplToClone);
        if (
            !templateTable.Items.TryGetValue(cloneTplId, out var itemToClone)
            || itemToClone == null
        )
            throw new InvalidOperationException(
                $"Unable to find item to clone: {config.ItemTplToClone}"
            );

        var itemClone = cloner.Clone(itemToClone);
        itemClone.Id = newItemId;
        itemClone.Parent = NameHelper.ResolveId(config.ParentId, ItemMaps.ItemBaseClassMap);

        itemRegistrationHelper.UpdateBaseItemPropertiesWithOverrides(
            config.OverrideProperties,
            itemClone
        );

        itemRegistrationHelper.AddToItemsDb(newItemId, itemClone);

        var registerInHandbook = config.RegisterInHandbook ?? true;
        var registerInFleaPrices = config.RegisterInFleaPrices ?? true;

        if (registerInHandbook)
        {
            itemRegistrationHelper.AddToHandbookDb(
                newItemId,
                NameHelper.ResolveId(config.HandbookParentId!, ItemMaps.ItemHandbookCategoryMap),
                config.HandbookPriceRoubles!.Value
            );
        }

        itemRegistrationHelper.AddToLocaleDbs(config.Locales, newItemId);

        if (registerInFleaPrices)
        {
            itemRegistrationHelper.AddToFleaPriceDb(newItemId, config.FleaPriceRoubles!.Value);
        }

        itemRegistrationHelper.AddItemToBaseClassCache(assembly, newItemId);

        if (itemHelper.IsOfBaseclass(itemClone.Id, BaseClasses.WEAPON))
        {
            itemRegistrationHelper.AddToWeaponShelf(newItemId);
        }
    }

    private void ProcessAdditionalProperties(string newItemId, CustomItemConfig config)
    {
        if (config is { AddToTraders: true, Traders: not null })
            traderItemHelper.AddItem(config, newItemId);

        if (config.AddWeaponPreset == true)
            weaponPresetHelper.ProcessWeaponPresets(config, newItemId);

        if (config is { Masteries: true, MasterySections: not null })
            masteryHelper.AddOrUpdateMasteries(config.MasterySections, newItemId);

        if (config.AddToModSlots == true)
            AddDeferredModSlot(newItemId, config);

        if (config.AddToInventorySlots != null)
            inventorySlotHelper.ProcessInventorySlots(config, newItemId);

        if (config.AddToHallOfFame == true)
            hallOfFameHelper.AddToHallOfFame(config, newItemId);

        if (config.AddToSpecialSlots == true)
            specialSlotsHelper.AddToSpecialSlots(config, newItemId);

        if (config is { AddToStaticLootContainers: true, StaticLootContainers: not null })
            staticLootHelper.ProcessStaticLootContainers(config, newItemId);

        if (config.AddToBots == true)
            botLootHelper.AddToBotLoot(config, newItemId);

        if (config.AddCaliberToAllCloneLocations == true)
            AddDeferredCaliberConfig(newItemId, config);

        if (config is { AddToGeneratorAsFuel: true, GeneratorFuelSlotStages: not null })
            generatorFuelHelper.AddGeneratorFuel(config, newItemId);

        if (config.AddToHideoutPosterSlots == true)
            hideoutPosterHelper.AddToPosterSlot(newItemId);

        if (config is { AddPosterToMaps: true, PosterSpawnProbability: not null })
            posterLootHelper.ProcessPosterLoot(config, newItemId);

        if (config.AddToStatuetteSlots == true)
            hideoutStatuetteHelper.AddToStatuetteSlot(newItemId);

        if (config.AddToStaticAmmo == true)
            staticAmmoHelper.AddAmmoToLocationStaticAmmo(config, newItemId);

        if (config.AddToEmptyPropSlots == true)
            emptyPropSlotHelper.AddCustomSlots(config, newItemId);

        if (config.AddToSecureFilters == true)
            AddDeferredSecureFilters(newItemId, config);

        if (
            config is { ParentId: "62f109593b54472778797866", IsRandomLootContainer: true }
            && config.RandomLootContainerRewards != null
        )
            randomLootContainerHelper.ConfigureRandomLootContainer(config, newItemId);

        if (config.AddToItemBlacklist == true)
            rewardItemBlacklistHelper.AddToItemBlacklist(newItemId);
    }

    private void AddDeferredCaliberConfig(string newItemId, CustomItemConfig config)
    {
        if (_deferredCaliberConfigs.Any(d => d.newItemId == newItemId))
        {
            logger.Warning($"Deferred caliber config for {newItemId} already exists, skipping.");
            return;
        }

        _deferredCaliberConfigs.Add((newItemId, config));
    }

    public void ProcessDeferredCalibers()
    {
        if (_deferredCaliberConfigs.Count == 0)
        {
            LogHelper.Debug(logger, "No deferred caliber configs to process");
            return;
        }

        LogHelper.Debug(
            logger,
            $"Processing {_deferredCaliberConfigs.Count} deferred caliber configs..."
        );

        foreach (var (newItemId, config) in _deferredCaliberConfigs)
            try
            {
                caliberHelper.ProcessCaliberConfig(config, newItemId);

                LogHelper.Debug(logger, $"Processed caliber config for {newItemId}");
            }
            catch (Exception ex)
            {
                logger.Critical($"Failed processing caliber config for {newItemId}", ex);
            }

        _deferredCaliberConfigs.Clear();

        LogHelper.Debug(logger, "Finished processing deferred caliber configs");
    }

    private void AddDeferredModSlot(string newItemId, CustomItemConfig config)
    {
        if (_deferredModSlotConfigs.Any(d => d.newItemId == newItemId))
        {
            logger.Warning($"Deferred modslot for {newItemId} already exists, skipping.");
            return;
        }

        _deferredModSlotConfigs.Add((newItemId, config));
    }

    public void ProcessDeferredModSlots()
    {
        if (_deferredModSlotConfigs.Count == 0)
        {
            LogHelper.Debug(logger, "No deferred modslots to process");
            return;
        }

        LogHelper.Debug(logger, $"Processing {_deferredModSlotConfigs.Count} deferred modslots...");

        foreach (var (newItemId, config) in _deferredModSlotConfigs)
            try
            {
                modSlotHelper.ProcessModSlots(config, newItemId);

                LogHelper.Debug(logger, $"Processed modslots for {newItemId}");
            }
            catch (Exception ex)
            {
                logger.Critical($"Failed processing modslots for {newItemId}", ex);
            }

        _deferredModSlotConfigs.Clear();

        LogHelper.Debug(logger, "Finished processing deferred modslots");
    }

    private void AddDeferredSecureFilters(string newItemId, CustomItemConfig config)
    {
        if (_deferredSecureFilterConfigs.Any(d => d.newItemId == newItemId))
        {
            logger.Warning($"Deferred secure filters for {newItemId} already exists, skipping.");
            return;
        }

        _deferredSecureFilterConfigs.Add((newItemId, config));
    }

    public void ProcessDeferredSecureFilters()
    {
        if (_deferredSecureFilterConfigs.Count == 0)
        {
            LogHelper.Debug(logger, "No deferred secure filters to process");
            return;
        }

        LogHelper.Debug(
            logger,
            $"Processing {_deferredSecureFilterConfigs.Count} deferred secure filters..."
        );

        foreach (var (newItemId, config) in _deferredSecureFilterConfigs)
            try
            {
                secureFiltersHelper.AddToSecureFilters(config, newItemId);

                LogHelper.Debug(logger, $"Processed secure filters for {newItemId}");
            }
            catch (Exception ex)
            {
                logger.Critical($"Failed processing secure filters for {newItemId}", ex);
            }

        _deferredSecureFilterConfigs.Clear();

        LogHelper.Debug(logger, "Finished processing deferred secure filters");
    }
}
