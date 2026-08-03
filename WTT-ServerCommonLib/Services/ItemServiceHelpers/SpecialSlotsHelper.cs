using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class SpecialSlotsHelper(ISptLogger<SpecialSlotsHelper> logger, TemplateTable templateTable)
{
    public void AddToSpecialSlots(CustomItemConfigBase itemConfig, string itemId)
    {
        if (itemConfig.AddToSpecialSlots != true)
            return;

        var pocketIds = new[]
        {
            "627a4e6b255f7527fb05a0f6", // normal pockets
            "65e080be269cbd5c5005e529", // unheard pockets
        };

        var items = templateTable.Items;
        foreach (var pocketsId in pocketIds)
        {
            if (!items.TryGetValue(pocketsId, out var pockets))
            {
                logger.Warning(
                    $"[SpecialSlots] Could not find pockets template with id {pocketsId}"
                );
                continue;
            }

            if (pockets.Properties?.Slots == null)
            {
                logger.Warning($"[SpecialSlots] Pockets template {pocketsId} has no slots.");
                continue;
            }

            foreach (var slot in pockets.Properties.Slots)
            {
                if (slot.Properties?.Filters == null)
                    continue;

                var firstFilter = slot.Properties.Filters.FirstOrDefault();
                if (firstFilter?.Filter == null)
                    continue;

                if (firstFilter.Filter.Add(itemId))
                    LogHelper.Debug(
                        logger,
                        $"[SpecialSlots] Added {itemId} to pockets slot in {pocketsId}"
                    );
            }
        }
    }
}
