using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BepInEx.Logging;
using EFT;
using EFT.UI.DragAndDrop;
using SPT.Custom.Utils;
using UnityEngine;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;
using WTTClientCommonLib.Services;
using Object = UnityEngine.Object;

namespace WTTClientCommonLib;

public class ResourceLoader(ManualLogSource logger, AssetLoader assetLoader)
{
    public static Dictionary<string, Sprite> _customHideoutIcons = new();
    public static bool _iconsLoaded = false;
    public static Dictionary<string, Texture2D> _customMarkTextures = new();
    public static bool _texturesLoaded = false;
    public static AudioManifest Manifest { get; private set; } = new();
    public static AudioClipCache ClipCache { get; private set; }

    static ResourceLoader()
    {
        LogHelper.LogDebug("ResourceLoader static constructor: Creating AudioClipCache");
        var go = new GameObject("WTT_AudioClipCache");
        Object.DontDestroyOnLoad(go);
        ClipCache = go.AddComponent<AudioClipCache>();
        ClipCache.hideFlags = HideFlags.DontUnloadUnusedAsset;
        LogHelper.LogDebug("AudioClipCache created and set to DontDestroyOnLoad");
    }

    public async void LoadAllResourcesFromServer()
    {
        try
        {
            LogHelper.LogDebug("Loading resources from server...");
            await assetLoader.InitializeBundlesAsync();
            LoadVoicesFromServer();
            LoadSlotImagesFromServer();
            LoadRigLayoutsFromServer();
            LoadAudioManifestFromServer();
            LoadCustomHideoutIconsFromServer();
            LoadCustomMarkTexturesFromServer();
            assetLoader.SpawnConfigs = assetLoader.FetchSpawnConfigs();
            LogHelper.LogDebug($"Loaded {assetLoader.SpawnConfigs.Count} spawn configurations");
            LogHelper.LogDebug("All resources loaded successfully from server");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading resources from server: {ex}");
        }
    }

    public async Task RegisterAudioBundlesAsync(List<string> audioBundleKeys)
    {
        try
        {
            LogHelper.LogDebug($"[AudioManager] Registering {audioBundleKeys.Count} audio bundles");

            if (BundleManager.Bundles.Keys.Count == 0)
            {
                await BundleManager.DownloadManifest();
            }

            foreach (var bundleKey in audioBundleKeys)
            {
                if (!BundleManager.Bundles.TryGetValue(bundleKey, out var bundleItem))
                {
                    LogHelper.LogWarn(
                        $"[AudioManager] Audio bundle '{bundleKey}' not found in manifest"
                    );
                    continue;
                }

                var bundlePath = BundleManager.GetBundleFilePath(bundleItem);

                if (!File.Exists(bundlePath))
                {
                    LogHelper.LogWarn($"[AudioManager] Bundle file not found: {bundlePath}");
                    continue;
                }

                try
                {
                    var bundle = AssetBundle.LoadFromFile(bundlePath);
                    if (bundle == null)
                    {
                        LogHelper.LogWarn($"[AudioManager] Failed to load bundle: {bundleKey}");
                        continue;
                    }

                    var clips = bundle.LoadAllAssets<AudioClip>();
                    foreach (var clip in clips)
                    {
                        clip.LoadAudioData();
                        ClipCache.CacheAudioClip(clip.name, clip);
                        LogHelper.LogDebug($"[AudioManager] Cached audio: {clip.name}");
                    }

                    LogHelper.LogDebug(
                        $"[AudioManager] Loaded {clips.Length} audio clips from {bundleKey}"
                    );
                }
                catch (Exception ex)
                {
                    LogHelper.LogError(
                        $"[AudioManager] Error loading audio bundle {bundleKey}: {ex}"
                    );
                }
            }

            LogHelper.LogDebug("[AudioManager] Audio bundle registration complete");
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[AudioManager] Error registering audio bundles: {ex}");
        }
    }

    public async Task LoadAudioManifestFromServer()
    {
        try
        {
            LogHelper.LogDebug("[AudioManager] Loading audio manifest from server...");

            var manifestResponse = Helpers.Utils.Get<AudioManifest>(
                "/wttcommonlib/audio/manifest/get"
            );
            if (manifestResponse == null)
            {
                LogHelper.LogWarn("[AudioManager] No audio manifest received from server");
                return;
            }

            Manifest = manifestResponse;
            LogHelper.LogDebug(
                $"[AudioManager] Loaded manifest with {Manifest.FaceCardMappings.Count} face entries"
            );
            LogHelper.LogDebug($"[AudioManager] Radio audio: {Manifest.RadioAudio.Count} tracks");

            var audioBundleKeys = manifestResponse.AudioBundles;
            await RegisterAudioBundlesAsync(audioBundleKeys);
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[AudioManager] Error loading audio manifest: {ex}");
        }
    }

    public static string GetAudioForFace(string faceName)
    {
        LogHelper.LogDebug($"[AudioManager] GetAudioForFace called with: {faceName}");

        if (Manifest?.FaceCardMappings.TryGetValue(faceName, out var faceEntry) == true)
        {
            if (faceEntry.Audio?.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, faceEntry.Audio.Count);
                var audioKey = faceEntry.Audio[idx];
                LogHelper.LogDebug($"[AudioManager] Selected audio key for {faceName}: {audioKey}");
                return audioKey;
            }
        }

        LogHelper.LogDebug($"[AudioManager] No audio found for face: {faceName}");
        return null;
    }

    public static List<string> GetRadioAudio()
    {
        LogHelper.LogDebug(
            $"[AudioManager] Returning {Manifest?.RadioAudio.Count ?? 0} radio tracks"
        );
        return Manifest?.RadioAudio ?? new List<string>();
    }

    private void LoadVoicesFromServer()
    {
        try
        {
            var voiceResponse = Helpers.Utils.Get<Dictionary<string, string>>(
                "/wttcommonlib/voices/get"
            );
            if (voiceResponse == null)
            {
                logger.LogWarning("No voice data received from server");
                return;
            }

            foreach (var kvp in voiceResponse)
                if (!InGameBundles.dictionary_0.ContainsKey(kvp.Key))
                {
                    InGameBundles.dictionary_0[kvp.Key] = kvp.Value;
                    LogHelper.LogDebug($"Added voice key: {kvp.Key}");
                }

            LogHelper.LogDebug($"Loaded {voiceResponse.Count} voice mappings from server");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading voices: {ex}");
        }
    }

    private void LoadSlotImagesFromServer()
    {
        try
        {
            var images = Helpers.Utils.Get<Dictionary<string, string>>(
                "/wttcommonlib/slotimages/get"
            );
            if (images == null)
            {
                logger.LogWarning("No slot images");
                return;
            }

            foreach (var kvp in images)
            {
                byte[] imageData;
                try
                {
                    imageData = Convert.FromBase64String(kvp.Value);
                }
                catch
                {
                    logger.LogWarning($"Invalid data for {kvp.Key}");
                    continue;
                }

                CreateAndRegisterSlotImage(imageData, kvp.Key);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading slot images: {ex}");
        }
    }

    public static async Task LoadCustomMarkTexturesFromServer()
    {
        try
        {
            LogHelper.LogDebug("[MarkTextures] Loading custom mark textures from server...");

            await ImageManager.DownloadMarkTexturesManifest();

            if (ImageManager.ShootingRangeMarks.Count == 0)
            {
                LogHelper.LogWarn("[MarkTextures] No textures in manifest");
                return;
            }

            LogHelper.LogDebug(
                $"[MarkTextures] Manifest contains {ImageManager.ShootingRangeMarks.Count} textures"
            );

            int loadedCount = 0;
            foreach (var (fileName, imageItem) in ImageManager.ShootingRangeMarks)
            {
                LogHelper.LogDebug(
                    $"[MarkTextures] Processing texture: {fileName}, ModPath: {imageItem.ModPath}"
                );

                try
                {
                    var texture = await ImageManager.LoadMarkTexture(fileName);
                    if (texture != null)
                    {
                        var iconId = Path.GetFileNameWithoutExtension(fileName);
                        texture.name = iconId;
                        _customMarkTextures[iconId] = texture;
                        loadedCount++;
                        LogHelper.LogDebug($"[MarkTextures] Loaded icon for item: {fileName}");
                    }
                    else
                    {
                        LogHelper.LogWarn(
                            $"[MarkTextures] LoadImage returned null for: {fileName}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError(
                        $"[MarkTextures] Error loading texture {fileName}: {ex.Message}"
                    );
                }
            }

            LogHelper.LogDebug(
                $"[MarkTextures] Custom hideout icons loaded successfully ({loadedCount} total)"
            );
            _iconsLoaded = true;
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[MarkTextures] Error loading custom hideout icons: {ex}");
        }
    }

    public static async Task LoadCustomHideoutIconsFromServer()
    {
        try
        {
            LogHelper.LogDebug(
                "[HideoutIcons] Loading custom hideout customization icons from server..."
            );

            await ImageManager.DownloadManifest();

            if (ImageManager.HideoutIcons.Count == 0)
            {
                LogHelper.LogWarn("[HideoutIcons] No icons in manifest");
                return;
            }

            LogHelper.LogDebug(
                $"[HideoutIcons] Manifest contains {ImageManager.HideoutIcons.Count} icons"
            );

            int loadedCount = 0;
            foreach (var (fileName, imageItem) in ImageManager.HideoutIcons)
            {
                LogHelper.LogDebug(
                    $"[HideoutIcons] Processing icon: {fileName}, ModPath: {imageItem.ModPath}"
                );

                try
                {
                    var texture = await ImageManager.LoadImage(fileName);
                    if (texture != null)
                    {
                        var sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );

                        var iconId = Path.GetFileNameWithoutExtension(fileName);
                        sprite.name = iconId;

                        _customHideoutIcons[iconId] = sprite;
                        loadedCount++;
                        LogHelper.LogDebug($"[HideoutIcons] Loaded icon for item: {iconId}");
                    }
                    else
                    {
                        LogHelper.LogWarn(
                            $"[HideoutIcons] LoadImage returned null for: {fileName}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogError(
                        $"[HideoutIcons] Error loading icon {fileName}: {ex.Message}"
                    );
                }
            }

            LogHelper.LogDebug(
                $"[HideoutIcons] Custom hideout icons loaded successfully ({loadedCount} total)"
            );
            _iconsLoaded = true;
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[HideoutIcons] Error loading custom hideout icons: {ex}");
        }
    }

    private void LoadRigLayoutsFromServer()
    {
        try
        {
            var bundleMap = Helpers.Utils.Get<Dictionary<string, string>>(
                "/wttcommonlib/riglayouts/get"
            );
            if (bundleMap == null)
            {
                logger.LogWarning("No rig layouts received from server");
                return;
            }

            LogHelper.LogDebug($"Received {bundleMap.Count} rig layouts from server");

            foreach (var kvp in bundleMap)
            {
                var bundleName = kvp.Key;
                var base64Data = kvp.Value;
                if (string.IsNullOrEmpty(base64Data))
                {
                    logger.LogWarning($"No data for rig layout: {bundleName}");
                    continue;
                }

                byte[] bundleData;
                try
                {
                    bundleData = Convert.FromBase64String(base64Data);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Base64 decode failed for rig layout {bundleName}: {ex}");
                    continue;
                }

                if (bundleData.Length == 0)
                {
                    logger.LogWarning($"Bundle data is empty for rig layout: {bundleName}");
                    continue;
                }

                LoadBundleFromMemory(bundleData, bundleName);
            }

            LogHelper.LogDebug($"Loaded {bundleMap.Count} rig layouts from server");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading rig layouts: {ex}");
        }
    }

    private void CreateAndRegisterSlotImage(byte[] data, string slotID)
    {
        try
        {
            if (data == null || data.Length == 0)
            {
                logger.LogWarning($"Empty data for slot image: {slotID}");
                return;
            }

            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(data))
            {
                logger.LogWarning($"Failed to create texture for {slotID}");
                return;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            sprite.texture.ignoreMipmapLimit = true;
            sprite.texture.mipMapBias = -1f;

            ResourceHelper.AddEntry($"Slots/{slotID}", sprite);
            LogHelper.LogDebug($"Added slot sprite: {slotID}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error creating slot sprite {slotID}: {ex}");
        }
    }

    private void LoadBundleFromMemory(byte[] data, string bundleName)
    {
        try
        {
            if (data == null || data.Length == 0)
            {
                logger.LogWarning($"Bundle data is null or empty for: {bundleName}");
                return;
            }

            var bundle = AssetBundle.LoadFromMemory(data);
            if (bundle == null)
            {
                logger.LogWarning($"Failed to load rig layout bundle: {bundleName}");
                return;
            }

            var loadedCount = 0;
            var gameObjects = bundle.LoadAllAssets<GameObject>();
            if (gameObjects == null || gameObjects.Length == 0)
                logger.LogWarning($"No GameObjects loaded from bundle: {bundleName}");

            if (gameObjects != null)
                foreach (var prefab in gameObjects)
                {
                    if (prefab == null)
                    {
                        logger.LogWarning("Encountered null prefab in bundle.");
                        continue;
                    }

                    var gridView = prefab.GetComponent<ContainedGridsView>();
                    if (gridView == null)
                    {
                        logger.LogWarning($"Prefab {prefab.name} missing ContainedGridsView.");
                        continue;
                    }

                    ResourceHelper.AddEntry($"UI/Rig Layouts/{prefab.name}", gridView);
                    loadedCount++;
                    LogHelper.LogDebug($"Added rig layout: {prefab.name}");
                }

            bundle.Unload(false);
            LogHelper.LogDebug($"Loaded {loadedCount} prefabs from bundle: {bundleName}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading bundle {bundleName}: {ex}");
        }
    }
}
