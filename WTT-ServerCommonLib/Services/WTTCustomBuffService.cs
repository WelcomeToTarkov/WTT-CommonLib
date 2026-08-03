using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomBuffService(
    ModHelper modHelper,
    ISptLogger<WTTCustomBuffService> logger,
    ConfigHelper configHelper,
    GlobalTable globalTable
)
{
    /// <summary>
    /// Loads custom stimulator buff configurations from JSON/JSONC files and registers them to the game database.
    ///
    /// Buffs are loaded from the mod's "db/CustomBuffs" directory (or a custom path if specified).
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomBuffs(Assembly assembly, string? relativePath = null)
    {
        var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        var defaultDir = Path.Combine("db", "CustomBuffs");
        var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

        if (!Directory.Exists(finalDir))
            throw new DirectoryNotFoundException($"Config directory not found at {finalDir}");

        var jsonFiles = Directory
            .GetFiles(finalDir, "*.json")
            .Concat(Directory.GetFiles(finalDir, "*.jsonc"))
            .ToArray();
        if (jsonFiles.Length == 0)
        {
            logger.Warning($"No CustomBuffs files found in {finalDir}");
            return;
        }

        var dbBuffs = globalTable.Configuration.Health.Effects.Stimulator.Buffs;
        var customBuffs = await configHelper.LoadAllJsonFiles<Dictionary<string, List<Buff>>>(
            finalDir
        );

        if (customBuffs.Count == 0)
        {
            logger.Warning($"No customBuffs could be loaded from {finalDir}");
            return;
        }

        foreach (var buffsList in customBuffs)
        {
            foreach (var kvp in buffsList)
            {
                try
                {
                    dbBuffs[kvp.Key] = kvp.Value;
                    LogHelper.Debug(logger, $"Successfully added new buff: {kvp.Key}");
                }
                catch (Exception ex)
                {
                    logger.Error($"Error adding buff {kvp.Key}: {ex.Message}");
                }
            }
        }
    }
}
