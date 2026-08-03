using SPTarkov.Common.Logger;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class RandomLootContainerHelper(
    SptLogger<RandomLootContainerHelper> logger,
    TemplateTable templateTable,
    InventoryConfig inventoryConfig
)
{
    public void ConfigureRandomLootContainer(CustomItemConfig itemConfig, string newItemId)
    {
        var itemDb = templateTable.Items;
        var itemInDb = itemDb.GetValueOrDefault(newItemId);
        if (itemInDb == null)
        {
            logger.Error("Item not found in db. Something is seriously wrong.");
            return;
        }
        itemInDb.Name = newItemId;
        inventoryConfig.RandomLootContainers[newItemId] =
            itemConfig.RandomLootContainerRewards
            ?? throw new ArgumentNullException(nameof(itemConfig.RandomLootContainerRewards));
    }
}
