using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomWeaponPresetService(
    ModHelper modHelper,
    ISptLogger<WTTCustomWeaponPresetService> logger,
    ConfigHelper configHelper,
    GlobalTable globalTable
)
{
    /// <summary>
    /// Loads custom weapon presets from JSON/JSONC files and registers them to the game database.
    ///
    /// Presets are loaded from the mod's "db/CustomWeaponPresets" directory (or a custom path if specified).
    ///
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomWeaponPresets(Assembly assembly, string? relativePath = null)
    {
        var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        var defaultDir = Path.Combine("db", "CustomWeaponPresets");
        var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

        if (!Directory.Exists(finalDir))
            throw new DirectoryNotFoundException($"Config directory not found at {finalDir}");

        var jsonFiles = Directory
            .GetFiles(finalDir, "*.json")
            .Concat(Directory.GetFiles(finalDir, "*.jsonc"))
            .ToArray();
        if (jsonFiles.Length == 0)
        {
            logger.Warning($"No CustomWeaponPresets files found in {finalDir}");
            return;
        }

        var itemPresets = globalTable.ItemPresets;
        var customPresets = await configHelper.LoadAllJsonFiles<Dictionary<string, Preset>>(
            finalDir
        );

        if (customPresets.Count == 0)
        {
            logger.Warning($"No customPresets could be loaded from {finalDir}");
            return;
        }

        foreach (var presetList in customPresets)
        {
            foreach (var kvp in presetList)
            {
                try
                {
                    var preset = kvp.Value;

                    if (preset.Items.Count == 0)
                    {
                        logger.Warning($"Preset {preset.Id} has no items defined. Skipping.");
                        continue;
                    }

                    itemPresets[preset.Id] = preset;
                    LogHelper.Debug(logger, $"Successfully added new weapon preset: {preset.Id}");
                }
                catch (Exception ex)
                {
                    logger.Error($"Error adding weapon preset {kvp.Key}: {ex.Message}");
                }
            }
        }
    }
}
