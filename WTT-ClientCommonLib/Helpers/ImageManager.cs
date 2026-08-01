using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using BepInEx.Logging;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Common.Utils;
using UnityEngine;
using WTTClientCommonLib.Models;
using Logger = BepInEx.Logging.Logger;

namespace SPT.Custom.Utils;

public enum ImageType
{
    HideoutIcon,
    ShootingRangeMark,
}

public static class ImageManager
{
    private const string BaseCachePath = "SPT_Runtime/user/cache/";
    private const string HideoutIconsCachePath = BaseCachePath + "hideouticons/";
    private const string ShootingRangeMarksCachePath = BaseCachePath + "shootingrangemarks/";

    private static readonly ManualLogSource LogHelper;
    public static readonly ConcurrentDictionary<string, ImageItem> HideoutIcons;
    public static readonly ConcurrentDictionary<string, ImageItem> ShootingRangeMarks;

    static ImageManager()
    {
        LogHelper = Logger.CreateLogSource(nameof(ImageManager));
        HideoutIcons = new ConcurrentDictionary<string, ImageItem>();
        ShootingRangeMarks = new ConcurrentDictionary<string, ImageItem>();
    }

    private static string GetCachePath(ImageType type) =>
        type switch
        {
            ImageType.HideoutIcon => HideoutIconsCachePath,
            ImageType.ShootingRangeMark => ShootingRangeMarksCachePath,
            _ => BaseCachePath,
        };

    private static string GetDownloadRoute(ImageType type) =>
        type switch
        {
            ImageType.HideoutIcon => "/files/image",
            ImageType.ShootingRangeMark => "/files/texture",
            _ => "/files/image",
        };

    public static string GetImagePath(ImageItem image, ImageType type)
    {
        return RequestHandler.IsLocal ? $"SPT_Runtime/{image.ModPath}/" : GetCachePath(type);
    }

    public static string GetImageFilePath(ImageItem image, ImageType type)
    {
        return GetImagePath(image, type) + image.FileName;
    }

    public static async Task DownloadManifest()
    {
        try
        {
            LogHelper.LogDebug("Downloading hideout icons manifest...");
            var json = await RequestHandler.GetJsonAsync("/wttcommonlib/hideout/icons/manifest");
            var images = JsonConvert.DeserializeObject<ImageItem[]>(json);

            if (images == null)
                return;

            foreach (var image in images)
            {
                HideoutIcons.TryAdd(image.FileName, image);
            }

            LogHelper.LogDebug($"Downloaded manifest for {images.Length} hideout icons");
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Error downloading hideout icons manifest: {ex.Message}");
        }
    }

    public static async Task DownloadMarkTexturesManifest()
    {
        try
        {
            LogHelper.LogDebug("Downloading shooting range marks manifest...");
            var json = await RequestHandler.GetJsonAsync(
                "/wttcommonlib/hideout/marktextures/manifest"
            );
            var textures = JsonConvert.DeserializeObject<ImageItem[]>(json);

            if (textures == null)
                return;

            foreach (var texture in textures)
            {
                ShootingRangeMarks.TryAdd(texture.FileName, texture);
            }

            LogHelper.LogDebug($"Downloaded manifest for {textures.Length} shooting range marks");
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Error downloading shooting range marks manifest: {ex.Message}");
        }
    }

    public static async Task DownloadImage(
        ImageItem image,
        ImageType type,
        System.Action<DownloadProgress> progressCallback = null
    )
    {
        try
        {
            var cachePath = GetCachePath(type);
            var filepath = cachePath + image.FileName;

            var directoryPath = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                LogHelper.LogDebug($"[DownloadImage] Created directory: {directoryPath}");
            }

            LogHelper.LogDebug(
                $"[DownloadImage] Downloading {image.FileName} ({type}) to {filepath}"
            );

            var route = GetDownloadRoute(type);
            var base64Data = await RequestHandler.GetJsonAsync($"{route}/{image.FileName}");

            if (string.IsNullOrEmpty(base64Data))
            {
                LogHelper.LogError($"[DownloadImage] Received empty data for {image.FileName}");
                return;
            }

            LogHelper.LogDebug(
                $"[DownloadImage] Received {base64Data.Length} chars of base64 for {image.FileName}"
            );

            try
            {
                var binaryData = Convert.FromBase64String(base64Data);
                LogHelper.LogDebug(
                    $"[DownloadImage] Decoded to {binaryData.Length} bytes, writing to disk..."
                );

                await File.WriteAllBytesAsync(filepath, binaryData);

                LogHelper.LogDebug(
                    $"[DownloadImage] Successfully wrote {binaryData.Length} bytes to {filepath}"
                );

                if (File.Exists(filepath))
                {
                    LogHelper.LogDebug($"[DownloadImage] File verified to exist at {filepath}");
                }
                else
                {
                    LogHelper.LogError(
                        $"[DownloadImage] File write failed - file does not exist at {filepath}"
                    );
                }
            }
            catch (FormatException ex)
            {
                LogHelper.LogError(
                    $"[DownloadImage] Failed to decode base64 for {image.FileName}: {ex.Message}"
                );
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[DownloadImage] Error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public static async Task<Texture2D?> LoadImage(string imageName)
    {
        if (!HideoutIcons.TryGetValue(imageName, out var image))
        {
            LogHelper.LogWarning($"Hideout icon not found in manifest: {imageName}");
            return null;
        }

        return await LoadImageInternal(imageName, image, ImageType.HideoutIcon);
    }

    public static async Task<Texture2D?> LoadMarkTexture(string textureName)
    {
        if (!ShootingRangeMarks.TryGetValue(textureName, out var texture))
        {
            LogHelper.LogWarning($"Shooting range mark not found in manifest: {textureName}");
            return null;
        }

        return await LoadImageInternal(textureName, texture, ImageType.ShootingRangeMark);
    }

    public static async Task<bool> ShouldAcquire(ImageItem image, ImageType type)
    {
        var fikaInstalled = WTTClientCommonLib.WTTClientCommonLib.FikaInstalled;

        // If Fika is installed, always download (headless client can't access local mod files)
        if (fikaInstalled)
        {
            LogHelper.LogDebug(
                $"[ShouldAcquire] Fika detected, forcing download for {image.FileName}"
            );

            var cachePath = GetCachePath(type);
            var cacheFilepath = cachePath + image.FileName;

            if (VFS.Exists(cacheFilepath))
            {
                var data = await VFS.ReadFileAsync(cacheFilepath);
                var crc = Crc32.HashToUInt32(data);

                if (crc == image.Crc)
                {
                    LogHelper.LogDebug(
                        $"[ShouldAcquire] CACHE: File up-to-date in cache: {image.FileName}"
                    );
                    return false;
                }

                LogHelper.LogDebug(
                    $"[ShouldAcquire] CACHE: File invalid, re-acquiring: {image.FileName}"
                );
                return true;
            }

            LogHelper.LogDebug($"[ShouldAcquire] CACHE: File missing, acquiring: {image.FileName}");
            return true;
        }

        var filepath = GetImageFilePath(image, type);

        if (VFS.Exists(filepath))
        {
            if (RequestHandler.IsLocal)
            {
                LogHelper.LogDebug($"[ShouldAcquire] MOD: Loading locally {image.FileName}");
                return false;
            }
            var data = await VFS.ReadFileAsync(filepath);
            var crc = Crc32.HashToUInt32(data);

            if (crc == image.Crc)
            {
                LogHelper.LogDebug($"[ShouldAcquire] CACHE: Loading locally {image.FileName}");
                return false;
            }

            LogHelper.LogDebug(
                $"[ShouldAcquire] CACHE: Image is invalid, (re-)acquiring {image.FileName}"
            );
            return true;
        }

        LogHelper.LogDebug($"[ShouldAcquire] CACHE: Image is missing, acquiring {image.FileName}");
        return true;
    }

    private static async Task<Texture2D?> LoadImageInternal(
        string imageName,
        ImageItem image,
        ImageType type
    )
    {
        try
        {
            var TEST_MODE_FORCE_DOWNLOAD = false;
            var fikaInstalled = WTTClientCommonLib.WTTClientCommonLib.FikaInstalled;

            var cachePath = GetCachePath(type);

            // Use cache if TEST_MODE or Fika is installed
            var filepath =
                (TEST_MODE_FORCE_DOWNLOAD || fikaInstalled)
                    ? (cachePath + image.FileName)
                    : // Force cache
                    GetImageFilePath(image, type); // Normal path

            LogHelper.LogDebug(
                $"[LoadImage] TEST_MODE: {TEST_MODE_FORCE_DOWNLOAD}, Fika: {fikaInstalled}, Type: {type}, FilePath: {filepath}, IsLocal: {RequestHandler.IsLocal}"
            );

            if (TEST_MODE_FORCE_DOWNLOAD)
            {
                if (VFS.Exists(filepath))
                {
                    LogHelper.LogDebug(
                        $"[LoadImage] TEST MODE: File already in cache, using cached version: {imageName}"
                    );
                }
                else
                {
                    LogHelper.LogDebug(
                        $"[LoadImage] TEST MODE: File not in cache, downloading {imageName}"
                    );
                    await DownloadImage(image, type);
                }
            }
            else if (fikaInstalled)
            {
                // Fika headless: always check cache, download if needed
                if (await ShouldAcquire(image, type))
                {
                    await DownloadImage(image, type);
                }
            }
            else if (!RequestHandler.IsLocal)
            {
                // Remote client: normal cache logic
                if (await ShouldAcquire(image, type))
                {
                    await DownloadImage(image, type);
                }
            }
            // else: Local non-Fika: use mod folder files directly

            if (!VFS.Exists(filepath))
            {
                LogHelper.LogError($"[LoadImage] File does not exist: {filepath}");
                return null;
            }

            var data = await VFS.ReadFileAsync(filepath);

            var textureObj = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (textureObj.LoadImage(data))
            {
                textureObj.Apply(true, true);
                return textureObj;
            }

            UnityEngine.Object.Destroy(textureObj);
            return null;
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Error loading image {imageName}: {ex.Message}");
            return null;
        }
    }
}
