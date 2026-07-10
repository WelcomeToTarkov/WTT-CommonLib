using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class HideoutStatuetteHelper(
    ISptLogger<HideoutStatuetteHelper> logger,
    TemplateTable templateTable
)
{
    private const string CustomizationItem = "673c7b00cbf4b984b5099181";

    private static readonly string?[] StatuetteSlotIds =
    [
        "Statuette_Gym_1",
        "Statuette_PlaceOfFame_1",
        "Statuette_PlaceOfFame_2",
        "Statuette_PlaceOfFame_3",
        "Statuette_Heating_1",
        "Statuette_Heating_2",
        "Statuette_Library_1",
        "Statuette_Library_2",
        "Statuette_RestSpace_1",
        "Statuette_RestSpace_2",
        "Statuette_MedStation_1",
        "Statuette_MedStation_2",
        "Statuette_Kitchen_1",
        "Statuette_Kitchen_2",
        "Statuette_BoozeGenerator_1",
        "Statuette_Workbench_1",
        "Statuette_IntelligenceCenter_1",
        "Statuette_ShootingRange_1",
    ];

    public void AddToStatuetteSlot(string itemId)
    {
        var items = templateTable.Items;
        foreach (var statuetteSlotId in StatuetteSlotIds)
        {
            if (
                !items.TryGetValue(CustomizationItem, out var statuetteParent)
                || statuetteParent.Properties?.Slots == null
            )
                continue;

            AddItemToStatuetteSlots(itemId, statuetteParent, statuetteSlotId);
        }
    }

    private void AddItemToStatuetteSlots(
        string itemId,
        TemplateItem statuetteItem,
        string? statuetteSlotId
    )
    {
        foreach (var slot in statuetteItem.Properties?.Slots!)
        {
            if (string.IsNullOrWhiteSpace(slot.Name) || slot.Properties?.Filters == null)
                continue;

            var slotType = GetMatchingSlotType(slot.Name);
            if (statuetteSlotId != slotType)
                continue;

            AddItemToFilters(itemId, slot, statuetteItem.Name);
        }
    }

    private void AddItemToFilters(string itemId, Slot slot, string? slotName)
    {
        foreach (var filter in slot.Properties?.Filters!)
        {
            filter.Filter ??= new HashSet<MongoId>();

            LogHelper.Debug(
                logger,
                filter.Filter.Add(itemId)
                    ? $"[Statuette] Added {itemId} to slot '{slot.Name}' in {slotName}"
                    : $"[Statuette] {itemId} already in slot '{slot.Name}'"
            );
        }
    }

    private string? GetMatchingSlotType(string slotName)
    {
        foreach (var type in StatuetteSlotIds)
            if (type != null && slotName.StartsWith(type, StringComparison.OrdinalIgnoreCase))
                return type;
        return null;
    }
}
