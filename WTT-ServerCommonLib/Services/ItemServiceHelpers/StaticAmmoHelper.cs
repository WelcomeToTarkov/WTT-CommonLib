using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class StaticAmmoHelper(
    ISptLogger<StaticAmmoHelper> logger,
    LocationTable locationTable,
    TemplateTable templateTable
)
{
    public void AddAmmoToLocationStaticAmmo(CustomItemConfig itemConfig, string newItemId)
    {
        try
        {
            var locations = locationTable.GetDictionary();

            // Extract caliber from override properties
            var caliber =
                itemConfig.OverrideProperties.Caliber
                ?? templateTable.Items[newItemId].Properties?.Caliber;
            if (string.IsNullOrEmpty(caliber))
            {
                logger.Warning(
                    $"Item {newItemId} has no Caliber property, cannot add to static ammo"
                );
                return;
            }

            var probability = itemConfig.StaticAmmoProbability ?? 0;

            LogHelper.Debug(logger, $"Adding ammo {newItemId} to all location static ammo pools");
            LogHelper.Debug(logger, $"  Caliber: {caliber}");
            LogHelper.Debug(logger, $"  Probability: {probability}");

            var locationsUpdated = 0;

            foreach (var (locationId, location) in locations)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (location.StaticAmmo == null)
                    continue;
                try
                {
                    var ammoList = location.StaticAmmo.TryGetValue(caliber, out var details)
                        ? details.ToList()
                        : new List<StaticAmmoDetails>();

                    if (ammoList.Any(a => a.Tpl == newItemId))
                    {
                        LogHelper.Debug(
                            logger,
                            $"Ammo {newItemId} already exists in {caliber} for {locationId}, skipping"
                        );
                        continue;
                    }

                    ammoList.Add(
                        new StaticAmmoDetails { Tpl = newItemId, RelativeProbability = probability }
                    );

                    location.StaticAmmo[caliber] = ammoList;

                    LogHelper.Debug(logger, $"Added {newItemId} to {caliber} in {locationId}");
                    locationsUpdated++;
                }
                catch (Exception ex)
                {
                    logger.Warning($"Error adding ammo to location {locationId}: {ex.Message}");
                    LogHelper.Debug(logger, $"Stack trace: {ex.StackTrace}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error adding ammo to location static ammo: {ex.Message}");
            LogHelper.Debug(logger, $"Stack trace: {ex.StackTrace}");
        }
    }
}
