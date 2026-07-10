using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services.ItemServiceHelpers;

[Injectable]
public class WeaponPresetHelper(ISptLogger<WeaponPresetHelper> logger, GlobalTable globalTable)
{
    public void ProcessWeaponPresets(CustomItemConfig itemConfig, string itemId)
    {
        var itemPresets = globalTable.ItemPresets;

        if (itemConfig.WeaponPresets == null || itemConfig.WeaponPresets.Count == 0)
        {
            logger.Warning($"WeaponPresets list is null or empty when trying {itemId}. Skipping.");
            return;
        }

        foreach (var preset in itemConfig.WeaponPresets)
        {
            if (preset.Items.Count == 0)
            {
                logger.Warning($"Preset {preset.Id} has no items defined. Skipping.");
                continue;
            }

            itemPresets[preset.Id] = preset;
        }
    }
}
