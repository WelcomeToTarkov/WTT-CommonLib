using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class HideoutPosterHelper(
    ISptLogger<HideoutPosterHelper> logger,
    TemplateTable templateTable
)
{
    private const string CustomizationItem = "673c7b00cbf4b984b5099181";

    private static readonly string?[] PosterSlotIds =
    [
        "Poster_Security_1",
        "Poster_Security_2",
        "Poster_Generator_1",
        "Poster_Generator_2",
        "Poster_ScavCase_1",
        "Poster_ScavCase_2",
        "Poster_Stash_1",
        "Poster_WaterCloset_1",
        "Poster_ShootingRange_1",
        "Poster_Workbench_1",
        "Poster_IntelligenceCenter_1",
        "Poster_Kitchen_1",
        "Poster_MedStation_1",
        "Poster_AirFilteringUnit_1",
        "Poster_RestSpace_1",
        "Poster_RestSpace_2",
        "Poster_RestSpace_3",
        "Poster_RestSpace_4",
        "Poster_Heating_1",
        "Poster_Heating_2",
        "Poster_Heating_3",
        "Poster_Gym_1",
        "Poster_Gym_2",
        "Poster_Gym_3",
        "Poster_Gym_4",
        "Poster_Gym_5",
        "Poster_Gym_6",
        "Poster_Security_3",
        "Poster_ShootingRange_2",
    ];

    public void AddToPosterSlot(string itemId)
    {
        var items = templateTable.Items;
        foreach (var posterSlotId in PosterSlotIds)
        {
            if (
                !items.TryGetValue(CustomizationItem, out var posterParent)
                || posterParent.Properties?.Slots == null
            )
                continue;

            AddItemToPosterSlots(itemId, posterParent, posterSlotId);
        }
    }

    private void AddItemToPosterSlots(string itemId, TemplateItem posterItem, string? posterSlotId)
    {
        foreach (var slot in posterItem.Properties?.Slots!)
        {
            if (string.IsNullOrWhiteSpace(slot.Name) || slot.Properties?.Filters == null)
                continue;

            var slotType = GetMatchingSlotType(slot.Name);
            if (posterSlotId != slotType)
                continue;

            AddItemToFilters(itemId, slot, posterItem.Name);
        }
    }

    private void AddItemToFilters(string itemId, Slot slot, string? slotName)
    {
        foreach (var filter in slot.Properties?.Filters!)
        {
            filter.Filter ??= new HashSet<MongoId>();

            if (filter.Filter.Add(itemId))
                LogHelper.Debug(
                    logger,
                    $"[Poster] Added {itemId} to slot '{slot.Name}' in {slotName}"
                );
            else if (logger.IsLogEnabled(LogLevel.Debug))
                LogHelper.Debug(logger, $"[Poster] {itemId} already in slot '{slot.Name}'");
        }
    }

    private static string? GetMatchingSlotType(string slotName)
    {
        foreach (var type in PosterSlotIds)
            if (type != null && slotName.StartsWith(type, StringComparison.OrdinalIgnoreCase))
                return type;
        return null;
    }
}
