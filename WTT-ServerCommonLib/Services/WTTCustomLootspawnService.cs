using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomLootspawnService(
    ISptLogger<WTTCustomLootspawnService> logger,
    LocationTable locationTable,
    ConfigHelper configHelper,
    ModHelper modHelper
)
{
    private const double Epsilon = 0.0001;
    private readonly Dictionary<string, List<Spawnpoint>> _cachedForcedSpawns = new();

    private readonly Dictionary<string, List<Spawnpoint>> _cachedGeneralSpawns = new();
    private bool _transformersRegistered;

    /// <summary>
    /// Loads custom loot spawn configurations from JSON/JSONC files and registers them to map locations.
    ///
    /// General spawns are loaded from the mod's "db/CustomLootspawns/CustomSpawnpoints" directory.
    /// Forced spawns are loaded from the mod's "db/CustomLootspawns/CustomSpawnpointsForced" directory.
    /// Spawn configurations are organized by map name and merged with existing location data.
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomLootSpawns(Assembly assembly, string? relativePath = null)
    {
        try
        {
            var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            var baseDir = Path.Combine(
                assemblyLocation,
                relativePath ?? Path.Combine("db", "CustomLootspawns")
            );

            LogHelper.Debug(logger, $"Creating custom loot spawns from: {baseDir}");

            var spawnDir = Path.Combine(baseDir, "CustomSpawnpoints");
            var forcedDir = Path.Combine(baseDir, "CustomSpawnpointsForced");

            await GatherSpawnsFromDirectory(spawnDir, _cachedGeneralSpawns, "general");
            await GatherSpawnsFromDirectory(forcedDir, _cachedForcedSpawns, "forced");

            if (!_transformersRegistered)
            {
                RegisterTransformers();
                _transformersRegistered = true;
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to create custom loot spawns: {ex.Message}");
            LogHelper.Debug(logger, $"Stack trace: {ex.StackTrace}");
        }
    }

    private async Task GatherSpawnsFromDirectory(
        string directory,
        Dictionary<string, List<Spawnpoint>> cache,
        string spawnType
    )
    {
        if (!Directory.Exists(directory))
        {
            LogHelper.Debug(logger, $"Directory does not exist, skipping: {directory}");
            return;
        }

        try
        {
            LogHelper.Debug(logger, $"Gathering {spawnType} spawns from: {directory}");

            var spawnDicts = await configHelper.LoadAllJsonFiles<
                Dictionary<string, List<Spawnpoint>>
            >(directory);

            if (spawnDicts.Count == 0)
            {
                LogHelper.Debug(logger, $"No spawn configuration files found in: {directory}");
                return;
            }

            LogHelper.Debug(logger, $"Loaded {spawnDicts.Count} spawn configuration file(s)");

            foreach (var spawns in spawnDicts)
            {
                if (spawns.Count == 0)
                {
                    LogHelper.Debug(logger, "Empty spawn dictionary, skipping");
                    continue;
                }

                foreach (var (mapName, spawnList) in spawns)
                {
                    if (string.IsNullOrEmpty(mapName))
                    {
                        logger.Warning("Spawn configuration has null or empty map name, skipping");
                        continue;
                    }

                    if (spawnList.Count == 0)
                    {
                        LogHelper.Debug(logger, $"No spawn points for map '{mapName}', skipping");
                        continue;
                    }

                    var locationId = locationTable.GetMappedKey(mapName);

                    if (string.IsNullOrEmpty(locationId))
                    {
                        logger.Warning($"Could not map location name '{mapName}' to location ID");
                        continue;
                    }

                    if (!cache.ContainsKey(locationId))
                        cache[locationId] = new List<Spawnpoint>();

                    cache[locationId].AddRange(spawnList);
                    LogHelper.Debug(
                        logger,
                        $"Cached {spawnList.Count} {spawnType} spawn(s) for '{mapName}' (ID: {locationId})"
                    );
                }
            }

            LogHelper.Debug(
                logger,
                $"Finished gathering {spawnType} spawns: {cache.Count} location(s) in cache"
            );
        }
        catch (Exception ex)
        {
            logger.Error($"Error gathering spawns from directory '{directory}': {ex.Message}");
            LogHelper.Debug(logger, $"Stack trace: {ex.StackTrace}");
        }
    }

    private void RegisterTransformers()
    {
        try
        {
            var locations = locationTable.GetDictionary();

            LogHelper.Debug(logger, "Registering transformers for all locations");

            foreach (var (locationId, location) in locations)
            {
                location.LooseLoot?.AddTransformer(looseLoot =>
                {
                    if (looseLoot == null)
                        return looseLoot;

                    if (
                        _cachedGeneralSpawns.TryGetValue(locationId, out var generalSpawns)
                        && generalSpawns.Count > 0
                    )
                    {
                        looseLoot.Spawnpoints = MergeGeneral(
                            looseLoot.Spawnpoints,
                            generalSpawns,
                            locationId
                        );
                        LogHelper.Debug(
                            logger,
                            $"Applied {generalSpawns.Count} general spawn(s) to location '{locationId}'"
                        );
                    }

                    if (
                        _cachedForcedSpawns.TryGetValue(locationId, out var forcedSpawns)
                        && forcedSpawns.Count > 0
                    )
                    {
                        looseLoot.SpawnpointsForced = MergeForced(
                            looseLoot.SpawnpointsForced,
                            forcedSpawns,
                            locationId
                        );
                        LogHelper.Debug(
                            logger,
                            $"Applied {forcedSpawns.Count} forced spawn(s) to location '{locationId}'"
                        );
                    }

                    return looseLoot;
                });
            }

            var totalGeneral = _cachedGeneralSpawns.Values.Sum(list => list.Count);
            var totalForced = _cachedForcedSpawns.Values.Sum(list => list.Count);
            LogHelper.Debug(
                logger,
                $"Registered transformers for all locations ({totalGeneral} general, {totalForced} forced spawns cached)"
            );
        }
        catch (Exception ex)
        {
            logger.Error($"Error registering transformers: {ex.Message}");
            LogHelper.Debug(logger, $"Stack trace: {ex.StackTrace}");
        }
    }

    private List<Spawnpoint> MergeForced(
        IEnumerable<Spawnpoint>? existingForced,
        List<Spawnpoint> newSpawns,
        string locationId
    )
    {
        var existing = existingForced?.ToList() ?? new List<Spawnpoint>();

        try
        {
            var addedCount = 0;

            foreach (var newSpawn in newSpawns)
            {
                if (string.IsNullOrEmpty(newSpawn.LocationId))
                {
                    logger.Warning(
                        $"Spawn point missing LocationId in location '{locationId}', skipping"
                    );
                    continue;
                }

                if (existing.All(sp => sp.LocationId != newSpawn.LocationId))
                {
                    existing.Add(newSpawn);
                    addedCount++;
                }
            }

            if (addedCount > 0)
                LogHelper.Debug(
                    logger,
                    $"Merged {addedCount} new forced spawn(s) into location '{locationId}'"
                );

            return existing;
        }
        catch (Exception ex)
        {
            logger.Error($"Error merging forced spawns for location '{locationId}': {ex.Message}");
            return existing;
        }
    }

    private List<Spawnpoint> MergeGeneral(
        IEnumerable<Spawnpoint>? existingPoints,
        List<Spawnpoint> newSpawns,
        string locationId
    )
    {
        var existing = existingPoints?.ToList() ?? new List<Spawnpoint>();

        try
        {
            var addedCount = 0;
            var updatedCount = 0;

            foreach (var custom in newSpawns)
            {
                if (string.IsNullOrEmpty(custom.LocationId))
                {
                    logger.Warning(
                        $"Spawn point missing LocationId in location '{locationId}', skipping"
                    );
                    continue;
                }

                var match = existing.FirstOrDefault(sp => sp.LocationId == custom.LocationId);
                if (match == null)
                {
                    existing.Add(custom);
                    addedCount++;
                    continue;
                }

                match.Probability = custom.Probability;
                MergeSpawnpoint(match, custom, locationId);
                updatedCount++;
            }

            if (addedCount > 0 || updatedCount > 0)
                LogHelper.Debug(
                    logger,
                    $"Merged general spawns for location '{locationId}': {addedCount} added, {updatedCount} updated"
                );

            return existing;
        }
        catch (Exception ex)
        {
            logger.Error($"Error merging general spawns for location '{locationId}': {ex.Message}");
            return existing;
        }
    }

    private void MergeSpawnpoint(Spawnpoint existing, Spawnpoint custom, string locationId)
    {
        try
        {
            if (custom.Template == null)
                return;

            existing.Template ??= new SpawnpointTemplate();
            existing.Template.IsContainer = custom.Template.IsContainer;
            existing.Template.UseGravity = custom.Template.UseGravity;
            existing.Template.RandomRotation = custom.Template.RandomRotation;

            if (custom.Template.Items != null && custom.Template.Items.Any())
            {
                var items = existing.Template.Items?.ToList() ?? new List<SptLootItem>();

                foreach (var item in custom.Template.Items)
                    if (items.All(i => i.Id != item.Id))
                        items.Add(item);

                existing.Template.Items = items;
            }

            if (custom.Template.GroupPositions != null && custom.Template.GroupPositions.Any())
            {
                var groups =
                    existing.Template.GroupPositions?.ToList() ?? new List<GroupPosition>();

                foreach (var group in custom.Template.GroupPositions)
                {
                    if (group.Position == null)
                    {
                        logger.Warning(
                            $"Group position with null Position in spawn '{custom.LocationId}' for location '{locationId}', skipping"
                        );
                        continue;
                    }

                    var exists = groups.Any(g =>
                        AreEqual(g.Position?.X, group.Position?.X)
                        && AreEqual(g.Position?.Y, group.Position?.Y)
                        && AreEqual(g.Position?.Z, group.Position?.Z)
                    );

                    if (!exists)
                        groups.Add(group);
                }

                existing.Template.GroupPositions = groups;
            }

            if (custom.ItemDistribution != null && custom.ItemDistribution.Any())
            {
                var dists =
                    existing.ItemDistribution?.ToList() ?? new List<LooseLootItemDistribution>();

                foreach (var dist in custom.ItemDistribution)
                {
                    if (dist.ComposedKey == null)
                    {
                        logger.Warning(
                            $"Item distribution with null ComposedKey in spawn '{custom.LocationId}' for location '{locationId}', skipping"
                        );
                        continue;
                    }

                    if (dists.All(d => d.ComposedKey?.Key != dist.ComposedKey?.Key))
                        dists.Add(dist);
                }

                existing.ItemDistribution = dists.AsEnumerable();
            }
        }
        catch (Exception ex)
        {
            logger.Error(
                $"Error merging spawnpoint '{custom.LocationId}' in location '{locationId}': {ex.Message}"
            );
            LogHelper.Debug(logger, $"Stack trace: {ex.StackTrace}");
        }
    }

    private static bool AreEqual(double? a, double? b)
    {
        if (a == null || b == null)
            return Equals(a, b);
        return Math.Abs(a.Value - b.Value) < Epsilon;
    }
}
