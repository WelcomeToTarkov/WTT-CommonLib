using System.Reflection;
using SPTarkov.Common.Logger;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomQuestZoneService(
    ModHelper modHelper,
    SptLogger<WTTCustomQuestZoneService> logger,
    ConfigHelper configHelper
)
{
    private readonly Lock _lock = new();
    private readonly List<CustomQuestZone> _zones = new();

    /// <summary>
    /// Loads custom quest zones from JSON/JSONC files and registers them for quest interactions.
    ///
    /// Zones are loaded from the mod's "db/CustomQuestZones" directory (or a custom path if specified).
    ///
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomQuestZones(Assembly assembly, string? relativePath = null)
    {
        var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        var defaultDir = Path.Combine("db", "CustomQuestZones");
        var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

        if (!Directory.Exists(finalDir))
        {
            LogHelper.Debug(logger, $"No CustomQuestZones directory at {finalDir}");
            return;
        }

        var zones = await LoadZoneFiles(finalDir);
        RegisterZones(zones);
    }

    private void RegisterZones(IEnumerable<CustomQuestZone> zones)
    {
        lock (_lock)
        {
            var collection = zones.ToList();
            _zones.AddRange(collection);
            LogHelper.Debug(
                logger,
                $"Registered {collection.Count} zones. Total zones: {_zones.Count}"
            );
        }
    }

    public void RegisterZone(CustomQuestZone zone)
    {
        lock (_lock)
        {
            _zones.Add(zone);
            LogHelper.Debug(
                logger,
                $"Registered zone: {zone.ZoneName}. Total zones: {_zones.Count}"
            );
        }
    }

    private async Task<List<CustomQuestZone>> LoadZoneFiles(string directory)
    {
        var loadedZones = new List<CustomQuestZone>();

        var zoneLists = await configHelper.LoadAllJsonFiles<List<CustomQuestZone>>(directory);

        foreach (var fileZones in zoneLists)
            if (fileZones.Count > 0)
            {
                loadedZones.AddRange(fileZones);
                LogHelper.Debug(logger, $"Loaded {fileZones.Count} zones from a file");
            }

        return loadedZones;
    }

    internal IReadOnlyList<CustomQuestZone> GetZones()
    {
        return _zones;
    }
}
