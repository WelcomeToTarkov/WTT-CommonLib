using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class MasteryHelper(ISptLogger<MasteryHelper> logger, GlobalTable globalTable)
{
    public void AddOrUpdateMasteries(IEnumerable<Mastering> masterySections, string itemId)
    {
        var masteries = masterySections.ToList();
        if (masteries.Count == 0)
        {
            logger.Warning($"No mastery sections defined for item {itemId}");
            return;
        }

        foreach (var mastery in masteries)
        {
            if (string.IsNullOrEmpty(mastery.Name))
            {
                logger.Error("Mastery section has no name, skipping.");
                continue;
            }

            var existing = globalTable.Configuration.Mastering.FirstOrDefault(m =>
                m.Name.Equals(mastery.Name, StringComparison.OrdinalIgnoreCase)
            );

            if (existing != null)
            {
                var templates = existing.Templates.ToList();

                foreach (var template in mastery.Templates)
                {
                    if (string.IsNullOrEmpty(template))
                    {
                        logger.Warning("Invalid template in mastery section, skipping.");
                        continue;
                    }

                    if (!templates.Contains(template))
                    {
                        templates.Add(template);
                        LogHelper.Debug(
                            logger,
                            $"Added template {template} to mastery '{mastery.Name}'"
                        );
                    }
                }

                existing.Templates = templates.ToArray();
                LogHelper.Debug(
                    logger,
                    $"[Mastery] Updated existing mastery '{mastery.Name}' for {itemId}"
                );
            }
            else
            {
                var newMastery = new Mastering
                {
                    Name = mastery.Name,
                    Level2 = mastery.Level2,
                    Level3 = mastery.Level3,
                    Templates = mastery.Templates.ToArray(),
                };

                var newMastering = globalTable.Configuration.Mastering.ToList();
                newMastering.Add(newMastery);
                globalTable.Configuration.Mastering = newMastering.ToArray();

                LogHelper.Debug(
                    logger,
                    $"[Mastery] Created new mastery '{mastery.Name}' for {itemId}"
                );
            }
        }
    }
}
