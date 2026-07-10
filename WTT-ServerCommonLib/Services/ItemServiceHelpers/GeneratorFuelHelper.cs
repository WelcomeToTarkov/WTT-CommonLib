using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class GeneratorFuelHelper(ISptLogger<GeneratorFuelHelper> logger, HideoutTable hideoutTable)
{
    public void AddGeneratorFuel(CustomItemConfig itemConfig, string itemId)
    {
        var generator = hideoutTable.Areas.Find(a => a.Id == "5d3b396e33c48f02b81cd9f3");
        var validStages = itemConfig.GeneratorFuelSlotStages;

        if (generator == null)
        {
            logger.Error("Generator not found in hideout areas.");
            return;
        }

        if (generator.Stages != null)
            if (validStages != null)
                foreach (var validStage in validStages)
                {
                    if (!generator.Stages.TryGetValue(validStage, out Stage? stage))
                    {
                        logger.Error($"Stage {validStage} not found in generator fuel.");
                        continue;
                    }

                    if (stage.Bonuses != null)
                        foreach (var bonus in stage.Bonuses)
                        {
                            if (
                                bonus is not { Type: BonusType.AdditionalSlots, Filter: { } filter }
                            )
                                continue;
                            if (filter.Contains(itemId))
                                continue;

                            filter.Add(itemId);
                            LogHelper.Debug(
                                logger,
                                $"[GeneratorFuel] Added item {itemId} as fuel to generator at stage with bonus ID {bonus.Id}"
                            );
                        }
                }
    }
}
