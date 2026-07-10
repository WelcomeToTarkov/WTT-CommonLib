using SPTarkov.Common.Logger;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class BotLootHelper(BotTable botTable, SptLogger<BotLootHelper> logger)
{
    public void AddToBotLoot(CustomItemConfig itemConfig, string newItemId)
    {
        var cloneItemId = itemConfig.ItemTplToClone;

        foreach (var (_, bot) in botTable.Types)
        {
            var items = bot?.BotInventory.Items;
            if (items == null)
                continue;

            var containers = new[]
            {
                items.Backpack,
                items.Pockets,
                items.SecuredContainer,
                items.SpecialLoot,
                items.TacticalVest,
            };

            foreach (var container in containers)
            foreach (var (existingItem, chance) in container)
                if (existingItem.ToString() == cloneItemId)
                {
                    container[new MongoId(newItemId)] = chance;
                    LogHelper.Debug(
                        logger,
                        $"Added {newItemId} to {container[new MongoId(newItemId)]}"
                    );
                    break;
                }
        }
    }
}
