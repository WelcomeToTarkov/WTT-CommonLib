using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace WTTServerCommonLib.Helpers;

[Injectable]
public class SlotHelper
{
    public void ReplaceSlotFilters(TemplateItem item, int slotIndex, int filterIndex, HashSet<MongoId> ids)
    {
        var slot = GetSlotAtIndex(item, slotIndex);
        var filter = GetSlotFilterAtIndex(slot, filterIndex);

        filter.Filter = ids;
    }
    public void ModifySlotFilters(TemplateItem item, int slotIndex, int filterIndex, List<MongoId> ids, bool isCartridge = false)
    {
        var slot = GetSlotAtIndex(item, slotIndex, isCartridge);
        var filter = GetSlotFilterAtIndex(slot, filterIndex);

        filter.Filter!.UnionWith(ids);
    }
    private Slot GetSlotAtIndex(TemplateItem item, int index, bool isCartridge = false)
    {
        var slots = isCartridge ? item.Properties?.Cartridges?.ToArray() : item.Properties?.Slots?.ToArray();

        if (index >= 0 && index < slots?.Length)
        {
            return slots[index];
        }

        throw new IndexOutOfRangeException($"Index on item slot property `{item.Name}` is out of range");
    }
    public void AddIdsToNamedSlot(TemplateItem item, string slotName, params string[] ids)
    {
        item.Properties ??= new TemplateItemProperties();

        var slots = item.Properties.Slots?.ToList() ?? new List<Slot>();

        var slot = slots.FirstOrDefault(s => s.Name == slotName);
        if (slot == null)
        {
            slot = new Slot { Name = slotName };
            slots.Add(slot);
            item.Properties.Slots = slots;
        }

        slot.Properties ??= new SlotProperties();

        var filters = slot.Properties.Filters?.ToList() ?? new List<SlotFilter>();

        var filterContainer = filters.FirstOrDefault();
        if (filterContainer == null)
        {
            filterContainer = new SlotFilter();
            filters.Add(filterContainer);
            slot.Properties.Filters = filters;
        }

        filterContainer.Filter ??= new HashSet<MongoId>();

        foreach (var id in ids)
        {
            filterContainer.Filter.Add(id);
        }
    }
    private SlotFilter GetSlotFilterAtIndex(Slot slot, int index)
    {
        var slotFilter = slot.Properties?.Filters?.ToArray() ?? [];

        if (index >= 0 && index < slotFilter.Length)
        {
            return slotFilter[index];
        }

        throw new IndexOutOfRangeException($"Index on slot property `{slot.Name}` is out of range");
    }
}