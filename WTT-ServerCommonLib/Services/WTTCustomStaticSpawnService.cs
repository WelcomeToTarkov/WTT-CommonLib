using System.Reflection;
using SPTarkov.Common.Logger;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Utils;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomStaticSpawnService(
    ModHelper modHelper,
    SptLogger<WTTCustomStaticSpawnService> logger,
    JsonUtil jsonUtil
)
{
    private readonly Dictionary<string, Dictionary<string, string>> _modBundles = new();

    private readonly Dictionary<string, List<CustomSpawnConfig>> _modConfigs = new();

    /// <summary>
    /// Loads custom static spawn configs and asset bundles from JSON files and directories.
    ///
    /// Configs are loaded from the mod's "db/CustomStaticSpawns/CustomSpawnConfigs" directory.
    /// Bundles are loaded from the mod's "db/CustomStaticSpawns/StaticBundles" directory.
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder.</param>
    public async Task CreateCustomStaticSpawns(Assembly assembly, string? relativePath = null)
    {
        var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        var modKey = assembly.GetName().Name!;
        var defaultDir = Path.Combine("db", "CustomStaticSpawns");
        var bundlesDir = Path.Combine(
            assemblyLocation,
            relativePath ?? defaultDir,
            "StaticBundles"
        );
        var configsDir = Path.Combine(
            assemblyLocation,
            relativePath ?? defaultDir,
            "CustomSpawnConfigs"
        );

        if (Directory.Exists(bundlesDir))
        {
            if (!_modBundles.ContainsKey(modKey))
                _modBundles[modKey] = new Dictionary<string, string>();

            foreach (var file in Directory.GetFiles(bundlesDir, "*.bundle"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                _modBundles[modKey][name] = file;
                LogHelper.Debug(
                    logger,
                    $"[SpawnService] Registered bundle '{name}' for mod '{modKey}'"
                );
            }
        }
        else
        {
            LogHelper.Debug(
                logger,
                $"[SpawnService] No bundles dir at '{bundlesDir}' for mod '{modKey}'"
            );
        }

        if (Directory.Exists(configsDir))
        {
            if (!_modConfigs.ContainsKey(modKey))
                _modConfigs[modKey] = new List<CustomSpawnConfig>();

            foreach (var file in Directory.GetFiles(configsDir, "*.json"))
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var info =
                        jsonUtil.Deserialize<List<CustomSpawnConfig>>(json)
                        ?? new List<CustomSpawnConfig>();
                    _modConfigs[modKey].AddRange(info);
                    LogHelper.Debug(
                        logger,
                        $"[SpawnService] Loaded {info.Count} configs from '{file}'"
                    );
                }
                catch (Exception ex)
                {
                    logger.Error($"[SpawnService] Failed to parse configs in '{file}': {ex}");
                }
        }
        else
        {
            LogHelper.Debug(
                logger,
                $"[SpawnService] No configs dir at '{configsDir}' for mod '{modKey}'"
            );
        }
    }

    public List<string> GetBundleManifest()
    {
        return _modBundles.Values.SelectMany(d => d.Keys).Distinct().ToList();
    }

    public async Task<byte[]?> GetBundleData(string bundleName)
    {
        foreach (var modBundles in _modBundles.Values)
            if (modBundles.TryGetValue(bundleName, out var path) && File.Exists(path))
            {
                LogHelper.Debug(
                    logger,
                    $"[SpawnService] Serving bundle '{bundleName}' from '{path}'"
                );
                return await File.ReadAllBytesAsync(path);
            }

        logger.Warning($"[SpawnService] Bundle '{bundleName}' not found");
        return null;
    }

    public List<CustomSpawnConfig> GetAllSpawnConfigs()
    {
        return _modConfigs.Values.SelectMany(list => list).ToList();
    }
}
