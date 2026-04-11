using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class ItemBlacklistHelper(
    ISptLogger<ItemBlacklistHelper> logger,
    ConfigServer configServer)
{
    private readonly ItemConfig _itemConfig = configServer.GetConfig<ItemConfig>();

    public void AddToItemBlacklist(string itemId)
    {
        MongoId mongoId = itemId;

        if (_itemConfig.Blacklist.Contains(mongoId))
        {
            logger.Warning($"Item {itemId} already in item blacklist, skipping.");
            return;
        }

        _itemConfig.Blacklist.Add(mongoId);
        LogHelper.Debug(logger, $"Added {itemId} to item blacklist.");
    }
}