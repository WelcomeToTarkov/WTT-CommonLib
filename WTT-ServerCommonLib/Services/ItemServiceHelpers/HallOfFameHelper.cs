using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class HallOfFameHelper(ISptLogger<HallOfFameHelper> logger, TemplateTable templateTable)
{
    private static readonly string?[] ValidTypes = ["dogtag", "smallTrophies", "bigTrophies"];

    private static readonly string[] HallItemIds =
    [
        "63dbd45917fff4dee40fe16e", // Level 1
        "65424185a57eea37ed6562e9", // Level 2
        "6542435ea57eea37ed6562f0", // Level 3
    ];

    public void AddToHallOfFame(CustomItemConfig itemConfig, string itemId)
    {
        var filterTypes = GetValidFilterTypes(itemConfig);
        if (filterTypes.Count == 0)
        {
            logger.Warning($"[HallOfFame] No valid slot types for {itemId}");
            return;
        }

        var items = templateTable.Items;
        foreach (var hallId in HallItemIds)
        {
            if (!items.TryGetValue(hallId, out var hallItem) || hallItem.Properties?.Slots == null)
                continue;

            AddItemToHallSlots(itemId, hallItem, filterTypes);
        }
    }

    private HashSet<string?> GetValidFilterTypes(CustomItemConfig config)
    {
        var types = new HashSet<string?>(StringComparer.OrdinalIgnoreCase);

        if (config.HallOfFameSlots == null)
            return types;

        foreach (var slot in config.HallOfFameSlots)
            if (string.Equals(slot, "all", StringComparison.OrdinalIgnoreCase))
                foreach (var type in ValidTypes)
                    types.Add(type);
            else
                foreach (var type in ValidTypes)
                    if (slot.Equals(type, StringComparison.OrdinalIgnoreCase))
                        types.Add(type);

        return types;
    }

    private void AddItemToHallSlots(
        string itemId,
        TemplateItem hallItem,
        HashSet<string?> filterTypes
    )
    {
        foreach (var slot in hallItem.Properties?.Slots!)
        {
            if (string.IsNullOrWhiteSpace(slot.Name) || slot.Properties?.Filters == null)
                continue;

            var slotType = GetMatchingSlotType(slot.Name);
            if (!filterTypes.Contains(slotType))
                continue;

            AddItemToFilters(itemId, slot, hallItem.Name);
        }
    }

    private string? GetMatchingSlotType(string slotName)
    {
        foreach (var type in ValidTypes)
            if (type != null && slotName.StartsWith(type, StringComparison.OrdinalIgnoreCase))
                return type;
        return null;
    }

    private void AddItemToFilters(string itemId, Slot slot, string? hallName)
    {
        foreach (var filter in slot.Properties?.Filters!)
        {
            filter.Filter ??= new HashSet<MongoId>();

            if (filter.Filter.Add(itemId))
                LogHelper.Debug(
                    logger,
                    $"[HallOfFame] Added {itemId} to slot '{slot.Name}' in {hallName}"
                );
            else if (logger.IsLogEnabled(LogLevel.Debug))
                LogHelper.Debug(logger, $"[HallOfFame] {itemId} already in slot '{slot.Name}'");
        }
    }
}
