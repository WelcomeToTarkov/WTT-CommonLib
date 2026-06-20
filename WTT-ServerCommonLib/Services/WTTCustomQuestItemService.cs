using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Reflection;
using WTTServerCommonLib.Constants;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using WTTServerCommonLib.Services.ItemServiceHelpers;
using LogLevel = SPTarkov.Server.Core.Models.Spt.Logging.LogLevel;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomQuestItemService(
    ISptLogger<WTTCustomQuestItemService> logger,
    DatabaseServer databaseServer,
    DatabaseService databaseService,
    ModHelper modHelper,
    ConfigHelper configHelper,
    ICloner cloner,
    InventorySlotHelper inventorySlotHelper,
    ModSlotHelper modSlotHelper,
    SpecialSlotsHelper specialSlotsHelper,
    SecureFiltersHelper secureFiltersHelper,
    ItemRegistrationHelper itemRegistrationHelper,
    ItemBlacklistHelper rewardItemBlacklistHelper
)
{
    private readonly List<(string newItemId, CustomQuestItemConfig config)> _deferredModSlotConfigs = new();
    private readonly List<(string newItemId, CustomQuestItemConfig config)> _deferredSecureFilterConfigs = new();

    private DatabaseTables? _database;

    public async Task CreateCustomQuestItems(Assembly assembly, string? relativePath = null)
    {
        if (_database == null)
            _database = databaseServer.GetTables();

        try
        {
            var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            var defaultDir = Path.Combine("db", "CustomQuestItems");
            var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

            if (!Directory.Exists(finalDir))
            {
                logger.Error($"Directory not found at {finalDir}");
                return;
            }

            var itemConfigDicts =
                await configHelper.LoadAllJsonFiles<Dictionary<string, CustomQuestItemConfig>>(finalDir);

            if (itemConfigDicts.Count == 0)
            {
                logger.Warning($"No valid quest item configs found in {finalDir}");
                return;
            }

            var totalItemsCreated = 0;

            foreach (var configDict in itemConfigDicts)
            {
                foreach (var (itemId, configData) in configDict)
                {
                    configData.Validate(itemId);

                    if (CreateQuestItemFromConfig(assembly, itemId, configData))
                        totalItemsCreated++;
                }
            }

            LogHelper.Debug(logger,
                $"Created {totalItemsCreated} custom quest items from {itemConfigDicts.Count} files");
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading quest item configs: {ex.Message}");
        }
    }

    private bool CreateQuestItemFromConfig(Assembly assembly, string newItemId, CustomQuestItemConfig config)
    {
        try
        {
            CreateQuestItemFromClone(assembly, newItemId, config);
            LogHelper.Debug(logger, $"Created quest item {newItemId}");

            ProcessAdditionalProperties(newItemId, config);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to create quest item {newItemId}: {ex.Message}");
            return false;
        }
    }

    private void CreateQuestItemFromClone(Assembly assembly, string newItemId, CustomQuestItemConfig config)
    {
        var tables = databaseService.GetTables();

        if (tables.Templates.Items.TryGetValue(newItemId, out var existingItem))
            throw new InvalidOperationException($"ItemId already exists. {existingItem.Name}");

        var cloneTplId = ItemTplResolver.ResolveId(config.ItemTplToClone);
        if (!tables.Templates.Items.TryGetValue(cloneTplId, out var itemToClone) || itemToClone == null)
            throw new InvalidOperationException($"Unable to find item to clone: {config.ItemTplToClone}");

        var itemClone = cloner.Clone(itemToClone);
        itemClone.Id = newItemId;
        itemClone.Parent = NameHelper.ResolveId(config.ParentId, ItemMaps.ItemBaseClassMap);

        config.OverrideProperties.QuestItem = true;

        var registerInHandbook = config.RegisterInHandbook ?? false;
        var registerInFleaPrices = config.RegisterInFleaPrices ?? false;

        itemRegistrationHelper.UpdateBaseItemPropertiesWithOverrides(config.OverrideProperties, itemClone);
        itemRegistrationHelper.AddToItemsDb(newItemId, itemClone);

        if (registerInHandbook)
        {
            itemRegistrationHelper.AddToHandbookDb(
                newItemId,
                NameHelper.ResolveId(config.HandbookParentId!, ItemMaps.ItemHandbookCategoryMap),
                config.HandbookPriceRoubles!.Value);
        }

        itemRegistrationHelper.AddToLocaleDbs(config.Locales, newItemId);

        if (registerInFleaPrices)
        {
            itemRegistrationHelper.AddToFleaPriceDb(newItemId, config.FleaPriceRoubles!.Value);
        }

        itemRegistrationHelper.AddItemToBaseClassCache(assembly, newItemId);
    }

    private void ProcessAdditionalProperties(string newItemId, CustomQuestItemConfig config)
    {
        if (_database == null)
            return;

        if (config.AddToModSlots == true)
            AddDeferredModSlot(newItemId, config);

        if (config.AddToInventorySlots != null)
            inventorySlotHelper.ProcessInventorySlots(config, newItemId);

        if (config.AddToSpecialSlots == true)
            specialSlotsHelper.AddToSpecialSlots(config, newItemId);

        if (config.AddToSecureFilters == true)
            AddDeferredSecureFilters(newItemId, config);

        rewardItemBlacklistHelper.AddToItemBlacklist(newItemId);
    }

    private void AddDeferredModSlot(string newItemId, CustomQuestItemConfig config)
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
        {
            try
            {
                if (_database == null)
                    return;

                modSlotHelper.ProcessModSlots(config, newItemId);

                if (logger.IsLogEnabled(LogLevel.Debug))
                    LogHelper.Debug(logger, $"Processed modslots for {newItemId}");
            }
            catch (Exception ex)
            {
                logger.Critical($"Failed processing modslots for {newItemId}", ex);
            }
        }

        _deferredModSlotConfigs.Clear();
        LogHelper.Debug(logger, "Finished processing deferred modslots");
    }

    private void AddDeferredSecureFilters(string newItemId, CustomQuestItemConfig config)
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

        LogHelper.Debug(logger, $"Processing {_deferredSecureFilterConfigs.Count} deferred secure filters...");

        foreach (var (newItemId, config) in _deferredSecureFilterConfigs)
        {
            try
            {
                if (_database == null)
                    return;

                secureFiltersHelper.AddToSecureFilters(config, newItemId);

                if (logger.IsLogEnabled(LogLevel.Debug))
                    LogHelper.Debug(logger, $"Processed secure filters for {newItemId}");
            }
            catch (Exception ex)
            {
                logger.Critical($"Failed processing secure filters for {newItemId}", ex);
            }
        }

        _deferredSecureFilterConfigs.Clear();
        LogHelper.Debug(logger, "Finished processing deferred secure filters");
    }
}