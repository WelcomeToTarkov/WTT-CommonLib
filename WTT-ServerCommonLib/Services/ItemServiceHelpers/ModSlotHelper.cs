using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class ModSlotHelper(ISptLogger<ModSlotHelper> logger, TemplateTable templateTable)
{
    public void ProcessModSlots(CustomItemConfigBase itemConfig, string newItemId)
    {
        var itemTplToClone = itemConfig.ItemTplToClone;
        var finalTplToClone = ItemTplResolver.ResolveId(itemTplToClone);
        if (
            itemConfig.AddToModSlots != true
            || itemConfig.ModSlot == null
            || itemConfig.ModSlot.Count == 0
        )
            return;

        var targetSlotNames = itemConfig.ModSlot.Select(slot => slot.ToLower()).ToList();

        var items = templateTable.Items;
        foreach (var (parentId, parentTemplate) in items)
        {
            if (parentTemplate.Properties?.Slots == null)
                continue;

            foreach (var slot in parentTemplate.Properties.Slots)
            {
                var slotNameLower = slot.Name?.ToLower();
                if (slotNameLower == null || !targetSlotNames.Contains(slotNameLower))
                    continue;

                var slotFilter = slot.Properties?.Filters?.FirstOrDefault();
                if (slotFilter?.Filter == null)
                    continue;

                if (slotFilter.Filter.Contains(finalTplToClone) && slotFilter.Filter.Add(newItemId))
                    LogHelper.Debug(
                        logger,
                        $"[ModSlots] Added {newItemId} to slot '{slot.Name}' for parent {parentId}"
                    );
            }
        }
    }
}
