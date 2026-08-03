using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Quests;
using Newtonsoft.Json;
using SPT.Custom.Utils;
using UnityEngine;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;
using Object = UnityEngine.Object;

namespace WTTClientCommonLib.Services;

public class AssetLoader(ManualLogSource logger)
{
    private readonly ConcurrentDictionary<string, AssetBundle> _loadedBundles = new();
    private readonly ConcurrentDictionary<string, string> _bundleKeyLookup = new();

    public List<CustomSpawnConfig> SpawnConfigs = null;

    public List<CustomSpawnConfig> FetchSpawnConfigs()
    {
        try
        {
            return Helpers.Utils.Get<List<CustomSpawnConfig>>(
                    "/wttcommonlib/spawnsystem/configs/get"
                ) ?? new List<CustomSpawnConfig>();
        }
        catch (Exception ex)
        {
            logger.LogError($"Error fetching configs: {ex}");
            return new List<CustomSpawnConfig>();
        }
    }

    public void ProcessSpawnConfig(Player player, CustomSpawnConfig config, string locationID)
    {
        try
        {
            if (
                string.IsNullOrEmpty(config.PrefabName)
                || string.IsNullOrEmpty(config.BundleName)
                || string.IsNullOrEmpty(config.LocationID)
            )
            {
                LogHelper.LogDebug(
                    $"[WTT-SpawnSystem] Invalid config: {JsonConvert.SerializeObject(config)}"
                );
                return;
            }

            // Location check
            if (!locationID.Equals(config.LocationID, StringComparison.OrdinalIgnoreCase))
                return;

            // Get quest reference
            QuestDataClass quest = null;
            if (!string.IsNullOrEmpty(config.QuestId))
                quest = player.Profile.QuestsData.FirstOrDefault(q => q.Id == config.QuestId);

            // Evaluate conditions (all must pass)
            if (!EvaluateConditions(player, quest, config))
            {
                LogHelper.LogDebug($"[WTT-SpawnSystem] Conditions not met for {config.PrefabName}");
                return;
            }

            // Load and spawn prefab
            var prefab = LoadPrefabFromBundle(config.BundleName, config.PrefabName);
            if (prefab == null)
                return;

            var rotation = Quaternion.Euler(config.Rotation);
            if (WTTClientCommonLib.FikaInstalled)
            {
                WTTClientCommonLib.SendFikaPacket(config, rotation);
            }

            SpawnPrefab(prefab, config.Position, rotation);

            LogHelper.LogDebug($"[WTT-SpawnSystem] Spawned {config.PrefabName}");
        }
        catch (Exception ex)
        {
            logger.LogError($"[WTT-SpawnSystem] Config processing failed: {ex}");
        }
    }

    private bool EvaluateConditions(Player player, QuestDataClass quest, CustomSpawnConfig config)
    {
        // Quest existence check
        if (config.QuestMustExist.HasValue)
        {
            var exists = quest != null;
            if (exists != config.QuestMustExist.Value)
            {
                LogHelper.LogDebug(
                    $"[CONDITION] Quest existence check failed. Expected: {config.QuestMustExist}, Actual: {exists}"
                );
                return false;
            }
        }

        // Required quest statuses (multiple)
        if (config.RequiredQuestStatuses is { Count: > 0 })
        {
            if (quest == null)
            {
                LogHelper.LogDebug("[CONDITION] Required statuses but quest doesn't exist");
                return false;
            }

            var anyMatch = false;
            var validStatuses = new List<string>();

            foreach (var statusStr in config.RequiredQuestStatuses)
                if (Enum.TryParse<EQuestStatus>(statusStr, out var requiredStatus))
                {
                    validStatuses.Add(statusStr);
                    if (quest.Status == requiredStatus)
                        anyMatch = true;
                }

            if (!anyMatch)
            {
                LogHelper.LogDebug(
                    $"[CONDITION] None of required statuses matched: {string.Join(", ", validStatuses)}. Actual: {quest.Status}"
                );
                return false;
            }
        }

        // Excluded quest statuses (multiple)
        if (config.ExcludedQuestStatuses is { Count: > 0 })
            if (quest != null)
                foreach (var statusStr in config.ExcludedQuestStatuses)
                    if (Enum.TryParse<EQuestStatus>(statusStr, out var excludedStatus))
                        if (quest.Status == excludedStatus)
                        {
                            LogHelper.LogDebug(
                                $"[CONDITION] Excluded status matched: {excludedStatus}"
                            );
                            return false;
                        }

        // Required item in inventory
        if (!string.IsNullOrEmpty(config.RequiredItemInInventory))
        {
            var hasItem = player.Profile.Inventory.AllRealPlayerItems.Any(item =>
                item.TemplateId == config.RequiredItemInInventory
            );

            if (!hasItem)
            {
                LogHelper.LogDebug(
                    $"[CONDITION] Missing required item: {config.RequiredItemInInventory}"
                );
                return false;
            }
        }

        // Required level
        if (config.RequiredLevel.HasValue)
            if (player.Profile.Info.Level < config.RequiredLevel.Value)
            {
                LogHelper.LogDebug(
                    $"[CONDITION] Level too low. Required: {config.RequiredLevel}, Actual: {player.Profile.Info.Level}"
                );
                return false;
            }

        // Required faction
        if (!string.IsNullOrEmpty(config.RequiredFaction))
        {
            var playerFaction = player.Profile.Side.ToString();
            if (!playerFaction.Equals(config.RequiredFaction, StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.LogDebug(
                    $"[CONDITION] Wrong faction. Required: {config.RequiredFaction}, Actual: {playerFaction}"
                );
                return false;
            }
        }

        // Enhanced linked quest condition
        if (!string.IsNullOrEmpty(config.LinkedQuestId))
        {
            var linkedQuest = player.Profile.QuestsData.FirstOrDefault(q =>
                q.Id == config.LinkedQuestId
            );

            // Existence check
            if (config.LinkedQuestMustExist.HasValue)
            {
                var linkedExists = linkedQuest != null;
                if (linkedExists != config.LinkedQuestMustExist.Value)
                {
                    LogHelper.LogDebug(
                        $"[CONDITION] Linked quest existence check failed. Expected: {config.LinkedQuestMustExist}, Actual: {linkedExists}"
                    );
                    return false;
                }
            }

            // Linked required statuses (multiple)
            if (config.LinkedRequiredStatuses is { Count: > 0 })
            {
                if (linkedQuest == null)
                {
                    LogHelper.LogDebug(
                        "[CONDITION] Required linked statuses but quest doesn't exist"
                    );
                    return false;
                }

                var anyMatch = false;
                var validStatuses = new List<string>();

                foreach (var statusStr in config.LinkedRequiredStatuses)
                    if (Enum.TryParse<EQuestStatus>(statusStr, out var requiredStatus))
                    {
                        validStatuses.Add(statusStr);
                        if (linkedQuest.Status == requiredStatus)
                            anyMatch = true;
                    }

                if (!anyMatch)
                {
                    LogHelper.LogDebug(
                        $"[CONDITION] None of linked required statuses matched: {string.Join(", ", validStatuses)}. Actual: {linkedQuest?.Status}"
                    );
                    return false;
                }
            }

            // Linked excluded statuses (multiple)
            if (config.LinkedExcludedStatuses is { Count: > 0 })
                if (linkedQuest != null)
                    foreach (var statusStr in config.LinkedExcludedStatuses)
                        if (Enum.TryParse<EQuestStatus>(statusStr, out var excludedStatus))
                            if (linkedQuest.Status == excludedStatus)
                            {
                                LogHelper.LogDebug(
                                    $"[CONDITION] Linked excluded status matched: {excludedStatus}"
                                );
                                return false;
                            }
        }

        // Boss spawn detection
        if (!string.IsNullOrEmpty(config.RequiredBossSpawned))
            if (!CheckBossSpawned(config.RequiredBossSpawned))
            {
                LogHelper.LogDebug(
                    $"[CONDITION] Required boss not spawned: {config.RequiredBossSpawned}"
                );
                return false;
            }

        // All conditions passed
        return true;
    }

    private bool CheckBossSpawned(string bossName)
    {
        try
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                LogHelper.LogDebug("[BOSS] GameWorld instance not found");
                return false;
            }

            foreach (var player in gameWorld.AllAlivePlayersList)
            {
                if (player.IsYourPlayer)
                    continue;

                if (player.AIData?.BotOwner?.Profile?.Info?.Settings?.Role == null)
                    continue;

                var roleName = player.AIData.BotOwner.Profile.Info.Settings.Role.ToString();

                if (roleName.Equals(bossName, StringComparison.OrdinalIgnoreCase))
                {
                    LogHelper.LogDebug($"[BOSS] Found {bossName} at {player.Transform.position}");
                    return true;
                }
            }

            LogHelper.LogDebug($"[BOSS] {bossName} not found in raid");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError($"[BOSS] Detection failed: {ex}");
            return false;
        }
    }

    public async Task InitializeBundlesAsync()
    {
        try
        {
            if (BundleManager.Bundles.Keys.Count == 0)
            {
                LogHelper.LogDebug("[AssetLoader] Downloading bundle manifest...");
                await BundleManager.DownloadManifest();
                LogHelper.LogDebug(
                    $"[AssetLoader] Manifest loaded with {BundleManager.Bundles.Count} bundles available"
                );
            }

            _bundleKeyLookup.Clear();
            foreach (var bundleKey in BundleManager.Bundles.Keys)
            {
                // Extract short name (last segment without .bundle)
                var shortName = Path.GetFileNameWithoutExtension(bundleKey);
                _bundleKeyLookup.TryAdd(shortName, bundleKey);
                LogHelper.LogDebug($"[AssetLoader] Registered bundle: {shortName} → {bundleKey}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"[AssetLoader] Error downloading bundle manifest: {ex}");
        }
    }

    public GameObject LoadPrefabFromBundle(string bundleIdentifier, string assetName)
    {
        try
        {
            var bundleKey = bundleIdentifier;

            if (!BundleManager.Bundles.ContainsKey(bundleKey))
            {
                if (_bundleKeyLookup.TryGetValue(bundleIdentifier, out var resolvedKey))
                {
                    bundleKey = resolvedKey;
                }
                else
                {
                    logger.LogError(
                        $"[AssetLoader] Bundle '{bundleIdentifier}' not found in manifest (tried as short name and full key)"
                    );
                    return null;
                }
            }

            if (!BundleManager.Bundles.TryGetValue(bundleKey, out var bundleItem))
            {
                logger.LogError($"[AssetLoader] Bundle key '{bundleKey}' not in BundleManager");
                return null;
            }

            var bundlePath = BundleManager.GetBundleFilePath(bundleItem);

            if (!File.Exists(bundlePath))
            {
                logger.LogError($"[AssetLoader] Bundle file not found at: {bundlePath}");
                return null;
            }

            if (!_loadedBundles.TryGetValue(bundleKey, out var bundle))
            {
                bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle == null)
                {
                    logger.LogError($"[AssetLoader] Failed to load bundle from: {bundlePath}");
                    return null;
                }

                _loadedBundles[bundleKey] = bundle;
                LogHelper.LogDebug($"[AssetLoader] Loaded bundle: {bundleKey} from {bundlePath}");
            }

            var prefab = bundle.LoadAsset<GameObject>(assetName);
            if (prefab == null)
            {
                logger.LogError(
                    $"[AssetLoader] Prefab '{assetName}' not found in bundle '{bundleKey}'"
                );
                return null;
            }

            return prefab;
        }
        catch (Exception ex)
        {
            logger.LogError($"[AssetLoader] Error loading prefab from bundle: {ex}");
            return null;
        }
    }

    private void SpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        try
        {
            Object.Instantiate(prefab, position, rotation);
            LogHelper.LogDebug($"[Spawner] Instantiated {prefab.name} at {position}");
        }
        catch (Exception ex)
        {
            logger.LogError($"[Spawner] Failed to instantiate prefab: {ex}");
        }
    }

    public void UnloadAllBundles()
    {
        if (_loadedBundles.Count == 0)
            return;

        foreach (var bundle in _loadedBundles.Values)
        {
            try
            {
                bundle.Unload(true);
            }
            catch (Exception ex)
            {
                logger.LogError($"[AssetLoader] Error unloading bundle: {ex}");
            }
        }

        _loadedBundles.Clear();
        LogHelper.LogDebug("[AssetLoader] All bundles unloaded");
    }
}
