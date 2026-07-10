using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using LogLevel = SPTarkov.Common.Models.Logging.LogLevel;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class PosterLootHelper(ISptLogger<PosterLootHelper> logger, LocationTable locationTable)
{
    public void ProcessPosterLoot(CustomItemConfig config, string itemId)
    {
        var locations = locationTable.GetDictionary();

        foreach (var (locationId, location) in locations)
        {
            if (location.LooseLoot is null)
                continue;

            location.LooseLoot.AddTransformer(lazyLoadedLooseLootData =>
            {
                foreach (var spawnpoint in lazyLoadedLooseLootData?.Spawnpoints ?? [])
                {
                    var template = spawnpoint.Template;

                    if (template is null)
                        continue;

                    var templateId = template.Id;
                    if (
                        string.IsNullOrEmpty(templateId)
                        || !templateId.StartsWith("flyer", StringComparison.OrdinalIgnoreCase)
                    )
                        continue;

                    var spawnPointItems = new List<SptLootItem>(template.Items ?? []);

                    if (spawnPointItems.Any(it => it.Template == itemId))
                        continue;

                    var itemDistList = new List<LooseLootItemDistribution>(
                        spawnpoint.ItemDistribution ?? []
                    );
                    var newId = new MongoId();

                    spawnPointItems.Add(
                        new SptLootItem
                        {
                            Id = newId,
                            Template = itemId,
                            ComposedKey = newId,
                            Upd = new Upd { StackObjectsCount = 1 },
                        }
                    );

                    itemDistList.Add(
                        new LooseLootItemDistribution
                        {
                            ComposedKey = new ComposedKey { Key = newId },
                            RelativeProbability = config.PosterSpawnProbability,
                        }
                    );

                    if (logger.IsLogEnabled(LogLevel.Debug))
                        LogHelper.Debug(
                            logger,
                            $"[PosterLoot] {locationId} + {spawnpoint.LocationId ?? "?"} id={templateId} key={newId}"
                        );

                    template.Items = spawnPointItems;
                    spawnpoint.ItemDistribution = itemDistList;
                }

                return lazyLoadedLooseLootData;
            });
        }
    }
}
