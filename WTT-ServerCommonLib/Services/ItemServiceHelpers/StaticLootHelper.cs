using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class StaticLootHelper(ISptLogger<StaticLootHelper> logger, LocationTable locationTable)
{
    public void ProcessStaticLootContainers(CustomItemConfig config, string itemId)
    {
        if (config.StaticLootContainers != null)
            foreach (var container in config.StaticLootContainers)
            {
                if (string.IsNullOrWhiteSpace(container.ContainerName))
                    continue;
                AddToStaticLoot(container.ContainerName, itemId, container.Probability);
            }
    }

    private void AddToStaticLoot(string containerId, string itemId, int probability)
    {
        var locations = locationTable.GetDictionary();

        if (locations.Count == 0)
        {
            logger.Warning("[StaticLoot] Locations dictionary was empty.");
            return;
        }

        foreach (var (locationId, location) in locations)
        {
            if (location.StaticLoot is null)
                continue;

            location.StaticLoot.AddTransformer(lazyloadedStaticLootData =>
            {
                if (lazyloadedStaticLootData is null)
                    return lazyloadedStaticLootData;

                var actualContainerId = ItemTplResolver.ResolveId(containerId);

                if (!actualContainerId.IsValidMongoId())
                {
                    logger.Error("[StaticLoot] Could not resolve container ID");
                    return lazyloadedStaticLootData;
                }

                if (
                    !lazyloadedStaticLootData.TryGetValue(
                        actualContainerId,
                        out var containerDetails
                    )
                )
                {
                    LogHelper.Debug(
                        logger,
                        $"[StaticLoot] Loot container '{containerId}' not found in {locationId}"
                    );
                    return lazyloadedStaticLootData;
                }

                AddDistributionToContainer(
                    containerDetails,
                    itemId,
                    probability,
                    locationId,
                    actualContainerId
                );

                return lazyloadedStaticLootData;
            });
        }
    }

    private void AddDistributionToContainer(
        StaticLootDetails? containerDetails,
        string itemId,
        int probability,
        string locationId,
        string containerId
    )
    {
        if (containerDetails is null)
        {
            logger.Warning($"[StaticLoot] Loot container '{containerId}' in {locationId} is null.");
            return;
        }

        var newItemDistribution = containerDetails.ItemDistribution.ToList();

        newItemDistribution.Add(
            new ItemDistribution { Tpl = itemId, RelativeProbability = probability }
        );

        containerDetails.ItemDistribution = newItemDistribution.ToArray();

        LogHelper.Debug(logger, "[StaticLoot] Successfully added item to static loot container!");
    }
}
