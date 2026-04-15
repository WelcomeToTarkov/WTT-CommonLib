using System.IO.Hashing;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using WTTServerCommonLib.Services;

namespace WTTServerCommonLib.Routes;

[Injectable]
public class WTTResourcesRouter(
    JsonUtil jsonUtil,
    WTTCustomQuestZoneService zoneService,
    WTTCustomRigLayoutService rigService,
    WTTCustomSlotImageService slotService,
    WTTCustomStaticSpawnService staticSpawnService,
    WTTCustomVoiceService voiceService,
    WTTCustomAudioService audioService,
    WTTCustomCustomizationService customizationService,
    WTTCustomHideoutRecipeService customHideoutRecipeService,
    ISptLogger<WTTResourcesRouter> logger) : StaticRouter(jsonUtil, [
    
    // Zones
    new RouteAction<EmptyRequestData>(
        "/wttcommonlib/zones/get", (_, _, _, _) =>
        {
            var zones = zoneService.GetZones();
            return ValueTask.FromResult(jsonUtil.Serialize(zones) ??
                                        throw new NullReferenceException("Could not serialize voice mappings!"));
        }
    ),

    new RouteAction<EmptyRequestData>(
        "/wttcommonlib/riglayouts/get", async (_, _, _, _) =>
        {
            var allBundles = rigService.GetLayoutManifest();
            var payload = new Dictionary<string, string>();
            foreach (var bundleName in allBundles)
            {
                var bundleData = await rigService.GetBundleData(bundleName);
                if (bundleData?.Length > 0)
                    payload.Add(bundleName, Convert.ToBase64String(bundleData));
            }

            return jsonUtil.Serialize(payload) ?? throw new NullReferenceException("Could not serialize payload!");
        }
    ),

    // Configs route
    new RouteAction<EmptyRequestData>(
        "/wttcommonlib/spawnsystem/configs/get", (_, _, _, _) =>
        {
            var configs = staticSpawnService.GetAllSpawnConfigs();
            return ValueTask.FromResult(jsonUtil.Serialize(configs) ?? string.Empty);
        }
    ),

    new RouteAction<EmptyRequestData>(
        "/wttcommonlib/slotimages/get", async (_, _, _, _) =>
        {
            var result = new Dictionary<string, string>();

            foreach (var name in slotService.GetImageManifest())
            {
                var data = await slotService.GetImageData(name);
                if (data?.Length > 0) result.Add(name, Convert.ToBase64String(data));
            }

            return jsonUtil.Serialize(result) ?? throw new NullReferenceException("Could not serialize payload!");
        }
    ),
    new RouteAction<EmptyRequestData>(
        "/wttcommonlib/hideout/icons/manifest", async (_, _, _, _) =>
        {
            var result = new List<ImageItem>();

            foreach (var iconName in customizationService.GetHideoutIconManifest())
            {
                var data = await customizationService.GetHideoutIconData(iconName);
                if (data?.Length > 0)
                {
                    var fullPath = customizationService.GetHideoutIconFullPath(iconName);
                
                    result.Add(new ImageItem
                    {
                        FileName = iconName + Path.GetExtension(fullPath), // Preserve actual extension
                        ModPath = Path.GetDirectoryName(fullPath).Replace("\\", "/"), // Directory path
                        Crc = Crc32.HashToUInt32(data)
                    });
                }
            }

            return jsonUtil.Serialize(result) ?? throw new NullReferenceException("Could not serialize image manifest!");
        }
    ),
    new RouteAction<EmptyRequestData>(
        "/wttcommonlib/hideout/marktextures/manifest", async (_, _, _, _) =>
        {
            var result = new List<ImageItem>();

            foreach (var textureName in customizationService.GetMarkTextureManifest())
            {
                var data = await customizationService.GetMarkTextureData(textureName);
                if (data?.Length > 0)
                {
                    var fullPath = customizationService.GetMarkTextureFullPath(textureName);
                
                    result.Add(new ImageItem
                    {
                        FileName = textureName + Path.GetExtension(fullPath),
                        ModPath = Path.GetDirectoryName(fullPath).Replace("\\", "/"),
                        Crc = Crc32.HashToUInt32(data)
                    });
                }
            }

            return jsonUtil.Serialize(result) ?? throw new NullReferenceException("Could not serialize mark textures manifest!");
        }
    ),


    // Voices
    new RouteAction<EmptyRequestData>(
        "/wttcommonlib/voices/get", (_, _, _, _) =>
        {
            var voiceMappings = voiceService.GetVoiceBundleMappings();
            return ValueTask.FromResult(jsonUtil.Serialize(voiceMappings) ??
                                        throw new NullReferenceException("Could not serialize voice mappings!"));
        }
    ),
    
    // CustomAudio
    new RouteAction<EmptyRequestData>(
    "/wttcommonlib/audio/manifest/get", (_, _, _, _) =>
    {
        var manifest = audioService.GetAudioManifest();
        return ValueTask.FromResult(jsonUtil.Serialize(manifest) ??
                                    throw new NullReferenceException("Could not serialize audio manifest!"));
    }
    ),
    
    // ExtendedRecipes
    new RouteAction<EmptyRequestData>(
        "/wttcommonlib/recipes/extended/get", (_, _, _, _) =>
        {
            var recipes = customHideoutRecipeService.GetExtendedRecipes();
            return ValueTask.FromResult(jsonUtil.Serialize(recipes) ??
                                        throw new NullReferenceException("Could not serialize extended recipes!"));
        }
    ),
]);



// In WTTResourcesRouterDynamic
[Injectable]
public class WTTResourcesRouterDynamic(
    JsonUtil jsonUtil,
    WTTCustomCustomizationService customizationService,
    ISptLogger<WTTResourcesRouterDynamic> logger) : DynamicRouter(jsonUtil, [
    new RouteAction<EmptyRequestData>(
        "/files/texture",
        async (url, _, sessionID, _) =>
        {
            try
            {
                var fileName = url.Split("/files/texture/").Last();
                var textureName = Path.GetFileNameWithoutExtension(fileName);

                var data = await customizationService.GetMarkTextureData(textureName);
                if (data == null || data.Length == 0)
                {
                    logger.Error($"[/files/texture] Texture not found: {textureName}");
                    return null;
                }

                return Convert.ToBase64String(data);
            }
            catch (Exception ex)
            {
                logger.Error($"[/files/texture] Error: {ex.Message}");
                return null;
            }
        }
    ),
    
    new RouteAction<EmptyRequestData>(
        "/files/image",
        async (url, _, sessionID, _) =>
        {
            try
            {
                var fileName = url.Split("/files/image/").Last();
                var iconName = Path.GetFileNameWithoutExtension(fileName);

                logger.Debug($"[/files/image] Requesting: {iconName}");

                var data = await customizationService.GetHideoutIconData(iconName);
                if (data == null || data.Length == 0)
                {
                    logger.Error($"[/files/image] Icon not found: {iconName}");
                    return null;
                }

                return Convert.ToBase64String(data);
            }
            catch (Exception ex)
            {
                logger.Error($"[/files/image] Error: {ex.Message}");
                return null;
            }
        }
    ),
]);


