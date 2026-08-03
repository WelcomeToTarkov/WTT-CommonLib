using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Items;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomItemParentService(
    ISptLogger<WTTCustomItemParentService> logger,
    TemplateTable templateTable,
    TradersTable tradersTable,
    ModHelper modHelper,
    ConfigHelper configHelper,
    ItemBaseClassService itemBaseClassService
)
{
    private readonly Dictionary<MongoId, CustomItemParentConfig> _loadedParents = [];
    private static readonly MongoId DefaultInventoryId = "55d7217a4bdc2d86028b456d";

    /// <summary>
    /// Loads custom parent configs from json and jsonc files and saves them in the spt database.
    /// Parents are loaded from "db/CustomParents" directory by default (or a custom path if specified).
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomParents(
        Assembly assembly,
        string relativePath = "db/CustomParents"
    )
    {
        string modDir = modHelper.GetAbsolutePathToModFolder(assembly);
        string parentDir = Path.Combine(modDir, relativePath);

        if (!Directory.Exists(parentDir))
        {
            logger.Error($"Could not find parent directory {relativePath}");
            return;
        }

        string[] files = Directory.GetFiles(parentDir, "*.json*", SearchOption.AllDirectories);

        foreach (string filePath in files)
        {
            var allParents = await configHelper.LoadJsonFileFlexible<
                Dictionary<MongoId, CustomItemParentConfig>
            >(filePath);

            if (allParents.Count == 0)
            {
                logger.Warning("No custom parents found");
                return;
            }

            foreach (Dictionary<MongoId, CustomItemParentConfig> parents in allParents)
            {
                foreach ((MongoId id, CustomItemParentConfig tpl) in parents)
                {
                    bool added = AddParentToDatabase(id, tpl);

                    if (added)
                    {
                        _loadedParents[id] = tpl;
                    }
                }
            }
        }
    }

    protected bool AddParentToDatabase(MongoId id, CustomItemParentConfig tpl)
    {
        try
        {
            var items = templateTable.Items;

            items.Add(id, tpl);
            itemBaseClassService.AddItemToCache(id);

            if (tpl.AddToContainers)
            {
                foreach (string containerNameOrId in tpl.Containers)
                {
                    AddParentToContainerFilter(id, containerNameOrId);
                }
            }

            if (tpl.AddToInventorySlots)
            {
                AddParentToInventorySlots(id, tpl.InventorySlots);
            }

            if (tpl.AddToTraderBuyLists)
            {
                AddParentToTraderBuyLists(id, tpl.Traders);
            }

            LogHelper.Debug(
                logger,
                $"Added parent {tpl.Id} to database, cache, and extra filters/buy lists"
            );

            return true;
        }
        catch (Exception e)
        {
            logger.Error(e.Message);
            return false;
        }
    }

    private void AddParentToContainerFilter(MongoId parentId, string containerNameOrId)
    {
        try
        {
            var actualContainerId = ItemTplResolver.ResolveId(containerNameOrId);

            Dictionary<MongoId, TemplateItem> items = templateTable.Items;
            items.TryGetValue(actualContainerId, out TemplateItem? container);

            if (container == null)
            {
                logger.Warning(
                    $"Could not find container '{containerNameOrId}' (resolved to {actualContainerId})"
                );
                return;
            }

            var firstGrid = container.Properties?.Grids?.FirstOrDefault();
            var firstFilter = firstGrid?.Properties?.Filters?.FirstOrDefault();

            if (firstFilter == null)
            {
                logger.Warning($"Container {actualContainerId} has no filters to modify");
                return;
            }

            firstFilter.Filter ??= [];
            if (!firstFilter.Filter.Contains(parentId))
            {
                firstFilter.Filter.Add(parentId);
                LogHelper.Debug(
                    logger,
                    $"Added parent {parentId} to container '{containerNameOrId}' ({actualContainerId}) filters"
                );
            }
        }
        catch (ArgumentException ex)
        {
            logger.Warning($"Failed to resolve container '{containerNameOrId}': {ex.Message}");
        }
    }

    private void AddParentToInventorySlots(MongoId parentId, IEnumerable<string> slotNames)
    {
        if (
            !templateTable.Items.TryGetValue(DefaultInventoryId, out var defaultInventory)
            || defaultInventory.Properties?.Slots == null
        )
        {
            logger.Warning($"Could not find default inventory {DefaultInventoryId}");
            return;
        }

        var targetNames =
            slotNames?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (targetNames.Count == 0)
        {
            logger.Warning(
                $"No inventory slots specified for parent {parentId}, skipping inventory slot injection"
            );
            return;
        }

        foreach (var slot in defaultInventory.Properties.Slots)
        {
            if (slot?.Name == null || !targetNames.Contains(slot.Name))
                continue;

            var firstFilter = slot.Properties?.Filters?.FirstOrDefault();
            if (firstFilter == null)
            {
                logger.Warning($"Slot {slot.Name} has no filters, cannot add parent {parentId}");
                continue;
            }

            firstFilter.Filter ??= [];
            if (!firstFilter.Filter.Contains(parentId))
            {
                firstFilter.Filter.Add(parentId);
                LogHelper.Debug(
                    logger,
                    $"Added parent {parentId} to inventory slot {slot.Name} filters"
                );
            }
        }
    }

    private void AddParentToTraderBuyLists(MongoId parentId, IEnumerable<string> traderNames)
    {
        if (traderNames == null)
        {
            logger.Warning(
                $"No traders specified for parent {parentId}, skipping trader buy list injection"
            );
            return;
        }

        foreach (var traderName in traderNames)
        {
            if (string.IsNullOrWhiteSpace(traderName))
                continue;

            if (!TraderIds.TraderMap.TryGetValue(traderName, out var traderId))
            {
                logger.Warning($"Unknown trader name '{traderName}' for parent {parentId}");
                continue;
            }

            if (!tradersTable.TryGetValue(traderId, out var trader))
            {
                logger.Warning(
                    $"Trader id {traderId} not found in database for name '{traderName}'"
                );
                continue;
            }

            var itemsBuy = trader.Base.ItemsBuy;
            if (itemsBuy?.Category == null)
            {
                logger.Warning($"Trader '{traderName}' ({traderId}) has no ItemsBuy.Category list");
                continue;
            }

            if (!itemsBuy.Category.Contains(parentId))
            {
                itemsBuy.Category.Add(parentId);
                LogHelper.Debug(
                    logger,
                    $"Added parent {parentId} to trader '{traderName}' ({traderId}) buy categories"
                );
            }
        }
    }

    public Dictionary<MongoId, CustomItemParentConfig> GetCustomParents()
    {
        return _loadedParents;
    }
}
