using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class TraderItemHelper(ISptLogger<TraderItemHelper> logger, DatabaseService databaseService)
{
    public void AddItem(CustomItemConfig config, string itemId)
    {
        try
        {
            if (config.Traders == null || config.Traders.Count == 0)
            {
                logger.Warning($"No trader entries for item {itemId}");
                return;
            }

            var traders = databaseService.GetTraders();
            foreach (var (traderKey, schemes) in config.Traders)
            {
                MongoId actualTraderId;

                if (TraderIds.TraderMap.TryGetValue(traderKey.ToLower(), out var traderId))
                {
                    actualTraderId = traderId;
                }
                else if (traderKey.IsValidMongoId())
                {
                    actualTraderId = traderKey;
                }
                else
                {
                    logger.Error($"Invalid trader key: {traderKey}");
                    continue;
                }
                
                if (!traders.TryGetValue(actualTraderId, out var trader))
                {
                    logger.Warning($"Trader not found in DB: ({actualTraderId})");
                    continue;
                }

                foreach (var (schemeKey, scheme) in schemes)
                {
                    var newItem = new Item
                    {
                        Id = schemeKey,
                        Template = itemId,
                        ParentId = "hideout",
                        SlotId = "hideout",
                        Upd = new Upd
                        {
                            UnlimitedCount = scheme.ConfigBarterSettings.UnlimitedCount,
                            StackObjectsCount = scheme.ConfigBarterSettings.StackObjectsCount,
                        }
                    };

                    if (scheme.ConfigBarterSettings.BuyRestrictionMax != null)
                    {
                        newItem.Upd.BuyRestrictionCurrent = 0;
                        newItem.Upd.BuyRestrictionMax = scheme.ConfigBarterSettings.BuyRestrictionMax;
                    }

                    trader.Assort.Items.Add(newItem);

                    if (!trader.Assort.BarterScheme.TryGetValue(schemeKey, out var barterOptions))
                    {
                        barterOptions = new List<List<BarterScheme>>();
                        trader.Assort.BarterScheme[schemeKey] = barterOptions;
                    }

                    var barters = scheme.Barters;
                    var barterSchemeItems = new List<BarterScheme>();

                    foreach (var b in barters)
                    {
                        if (string.IsNullOrWhiteSpace(b.Template)) continue;

                        var barter = new BarterScheme
                        {
                            Count = b.Count,
                            Template = ItemTplResolver.ResolveId(b.Template)
                        };

                        if (b.Level != null) barter.Level = b.Level;
                        if (b.OnlyFunctional != null) barter.OnlyFunctional = b.OnlyFunctional;
                        if (b.Side != null) barter.Side = b.Side;
                        if (b.SptQuestLocked != null) barter.SptQuestLocked = b.SptQuestLocked;

                        barterSchemeItems.Add(barter);
                    }

                    if (barterSchemeItems.Count > 0) barterOptions.Add(barterSchemeItems);

                    trader.Assort.LoyalLevelItems[schemeKey] = scheme.ConfigBarterSettings.LoyalLevel;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Critical($"Error adding {itemId} to traders", ex);
        }
    }
}