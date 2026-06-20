using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class SecureFiltersHelper(ISptLogger<SecureFiltersHelper> logger, DatabaseService databaseService)
{
    private const string SecureContainerParentId = "5448bf274bdc2dfc2f8b456a";
    private const string WTTSecureContainerParentId = "68154651f849fb4e7d816738";
    private const string BossContainerId = "5c0a794586f77461c458f892";

    public void AddToSecureFilters(CustomItemConfigBase itemConfig, string newItemId)
    {
        if (itemConfig.AddToSecureFilters != true)
            return;

        var items = databaseService.GetItems();

        foreach (var (_, itemTemplate) in items)
        {
            if (itemTemplate.Parent != SecureContainerParentId && itemTemplate.Parent != WTTSecureContainerParentId)
                continue;

            if (itemTemplate.Id == BossContainerId)
                continue;

            var grids = itemTemplate.Properties?.Grids?.ToList();
            if (grids == null || grids.Count == 0)
                continue;

            var gridFilters = grids[0].Properties?.Filters?.FirstOrDefault();
            if (gridFilters?.Filter == null)
            {
                logger.Warning(
                    $"[SecureFilters] Failed to add {newItemId} to secure container {itemTemplate.Id} filters (filters don't exist). " +
                    $"Check your SVM settings or load this mod before conflicting mods.");
                continue;
            }

            if (!gridFilters.Filter.Contains(newItemId))
            {
                gridFilters.Filter.Add(newItemId);
                LogHelper.Debug(logger,
                    $"[SecureFilters] Added {newItemId} to secure container {itemTemplate.Id}");
            }
        }
    }
}
