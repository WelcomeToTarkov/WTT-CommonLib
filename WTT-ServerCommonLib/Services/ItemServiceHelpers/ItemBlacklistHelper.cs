using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class ItemBlacklistHelper(ISptLogger<ItemBlacklistHelper> logger, ItemConfig itemConfig)
{
    public void AddToItemBlacklist(string itemId)
    {
        MongoId mongoId = itemId;

        if (itemConfig.Blacklist.Contains(mongoId))
        {
            logger.Warning($"Item {itemId} already in item blacklist, skipping.");
            return;
        }

        itemConfig.Blacklist.Add(mongoId);
        LogHelper.Debug(logger, $"Added {itemId} to item blacklist.");
    }
}
