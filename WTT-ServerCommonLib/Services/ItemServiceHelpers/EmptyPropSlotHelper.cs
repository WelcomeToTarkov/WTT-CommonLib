using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class EmptyPropSlotHelper(
    ISptLogger<EmptyPropSlotHelper> logger,
    TemplateTable templateTable
)
{
    public void AddCustomSlots(CustomItemConfig config, string itemToAdd)
    {
        var database = templateTable.Items;
        var itemToAddTo = config.EmptyPropSlot?.ItemToAddTo;
        var slotName = config.EmptyPropSlot?.ModSlot;

        foreach (var (key, value) in database)
        {
            if (key != itemToAddTo)
                continue;
            var slots = (List<Slot>)value?.Properties?.Slots!;

            if (slots.Count != 0)
                continue;
            var _slot = new Slot
            {
                Id = new MongoId(),
                MergeSlotWithChildren = false,
                Name = slotName,
                Parent = itemToAddTo,
                Properties = new SlotProperties
                {
                    Filters = new List<SlotFilter>
                    {
                        new SlotFilter { Filter = new HashSet<MongoId> { itemToAdd } },
                    },
                },
                Prototype = new MongoId(),
                Required = false,
            };

            slots.Add(_slot);
        }
    }
}
