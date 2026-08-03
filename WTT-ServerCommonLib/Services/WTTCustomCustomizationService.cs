using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Image;
using WTTServerCommonLib.Helpers;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomCustomizationService(
    ISptLogger<WTTCustomCustomizationService> logger,
    TemplateTable templateTable,
    HideoutTable hideoutTable,
    ModHelper modHelper,
    ConfigHelper configHelper,
    ImageRouterService imageRouterService
)
{
    private readonly Dictionary<string, string> _hideoutIconPaths = new();
    private readonly Dictionary<string, string> _markTexturePaths = new();

    /// <summary>
    /// Loads custom customization configs, customization storage entries, hideout customization configs,
    /// hideout customization icons and shooting range marks from JSON/JSONC files and image directories.
    ///
    /// Customizations are loaded from "db/CustomCustomization/Customization/" directory
    /// CustomizationStorage entries are loaded from "db/CustomCustomization/CustomizationStorage/" directory
    /// Hideout customizations are loaded from "db/CustomCustomization/HideoutCustomizationGlobals/" directory
    /// Hideout icons are loaded from "db/CustomCustomization/HideoutIcons/" directory
    /// Shooting range mark textures are loaded from "db/CustomCustomization/ShootingRangeMarkTextures/" directory
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="customizationRelativePath">(OPTIONAL) Custom path for customization configs relative to mod folder</param>
    /// <param name="storageRelativePath">(OPTIONAL) Custom path for customization storage configs relative to mod folder</param>
    /// <param name="hideoutCustomizationRelativePath">(OPTIONAL) Custom path for hideout customization configs relative to mod folder</param>
    /// <param name="hideoutIconsRelativePath">(OPTIONAL) Custom path for hideout icons relative to mod folder</param>
    /// <param name="markTexturesRelativePath">(OPTIONAL) Custom path for shooting range mark textures relative to mod folder</param>
    public async Task CreateCustomCustomizations(
        Assembly assembly,
        string? customizationRelativePath = null,
        string? storageRelativePath = null,
        string? hideoutCustomizationRelativePath = null,
        string? hideoutIconsRelativePath = null,
        string? markTexturesRelativePath = null
    )
    {
        try
        {
            var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);

            // Load customization items
            var customizationDir = Path.Combine(
                assemblyLocation,
                customizationRelativePath
                    ?? Path.Combine("db", "CustomCustomization", "Customization")
            );

            if (Directory.Exists(customizationDir))
            {
                var customizationCreated = await LoadAndRegisterCustomizations(customizationDir);
                LogHelper.Debug(
                    logger,
                    $"Created {customizationCreated} custom customization items"
                );
            }

            // Load customization storage entries
            var storageDir = Path.Combine(
                assemblyLocation,
                storageRelativePath
                    ?? Path.Combine("db", "CustomCustomization", "CustomizationStorage")
            );

            if (Directory.Exists(storageDir))
            {
                var storageCreated = await LoadAndRegisterCustomizationStorage(storageDir);
                LogHelper.Debug(
                    logger,
                    $"Created {storageCreated} custom customization storage entries"
                );
            }

            // Load hideout customization configs
            var hideoutCustomizationDir = Path.Combine(
                assemblyLocation,
                hideoutCustomizationRelativePath
                    ?? Path.Combine("db", "CustomCustomization", "HideoutCustomizationGlobals")
            );

            if (Directory.Exists(hideoutCustomizationDir))
            {
                var hideoutCustomizationCreated = await LoadAndRegisterHideoutCustomizations(
                    hideoutCustomizationDir
                );
                LogHelper.Debug(
                    logger,
                    $"Created {hideoutCustomizationCreated} custom hideout customization configs"
                );
            }

            // Load hideout customization icons
            var hideoutIconsDir = Path.Combine(
                assemblyLocation,
                hideoutIconsRelativePath
                    ?? Path.Combine("db", "CustomCustomization", "HideoutIcons")
            );

            if (Directory.Exists(hideoutIconsDir))
            {
                var iconsRegistered = LoadAndRegisterHideoutIcons(hideoutIconsDir);
                LogHelper.Debug(
                    logger,
                    $"Registered {iconsRegistered} hideout customization icons"
                );
            }

            // Load shooting range mark textures
            var markTexturesDir = Path.Combine(
                assemblyLocation,
                markTexturesRelativePath
                    ?? Path.Combine("db", "CustomCustomization", "ShootingRangeMarkTextures")
            );

            if (Directory.Exists(markTexturesDir))
            {
                var texturesRegistered = LoadAndRegisterMarkTextures(markTexturesDir);
                LogHelper.Debug(
                    logger,
                    $"Registered {texturesRegistered} shooting range mark textures"
                );
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading customization configs: {ex.Message}");
        }
    }

    private async Task<int> LoadAndRegisterCustomizations(string customizationDir)
    {
        var customizationConfigDicts = await configHelper.LoadAllJsonFiles<
            Dictionary<string, CustomizationItem>
        >(customizationDir);
        var totalCreated = 0;

        foreach (var configDict in customizationConfigDicts)
        {
            if (configDict.Count == 0)
                continue;

            foreach (var (customizationId, customizationItem) in configDict)
            {
                try
                {
                    templateTable.Customization[customizationId] = customizationItem;
                    totalCreated++;
                    LogHelper.Debug(logger, $"Registered customization item: {customizationId}");
                }
                catch (Exception ex)
                {
                    logger.Error(
                        $"Failed to register customization {customizationId}: {ex.Message}"
                    );
                }
            }
        }

        return totalCreated;
    }

    private async Task<int> LoadAndRegisterCustomizationStorage(string storageDir)
    {
        var storageConfigDicts = await configHelper.LoadAllJsonFiles<List<CustomisationStorage>>(
            storageDir
        );
        var totalCreated = 0;

        foreach (var configList in storageConfigDicts)
        {
            if (configList.Count == 0)
                continue;

            foreach (var storageEntry in configList)
            {
                try
                {
                    var customizationStorage = new CustomisationStorage
                    {
                        Id = storageEntry.Id,
                        Source = storageEntry.Source,
                        Type = storageEntry.Type,
                    };

                    templateTable.CustomisationStorage.Add(customizationStorage);
                    totalCreated++;
                    LogHelper.Debug(
                        logger,
                        $"Registered customization storage: {storageEntry.Id} ({storageEntry.Type})"
                    );
                }
                catch (Exception ex)
                {
                    logger.Error(
                        $"Failed to register customization storage {storageEntry.Id}: {ex.Message}"
                    );
                }
            }
        }

        return totalCreated;
    }

    private async Task<int> LoadAndRegisterHideoutCustomizations(string hideoutCustomizationDir)
    {
        var hideoutConfigDicts = await configHelper.LoadAllJsonFiles<
            List<HideoutCustomisationGlobal>
        >(hideoutCustomizationDir);
        var totalCreated = 0;

        foreach (var configDict in hideoutConfigDicts)
        {
            if (configDict.Count == 0)
                continue;

            foreach (HideoutCustomisationGlobal hideoutConfig in configDict)
            {
                try
                {
                    hideoutTable.Customisation.Globals?.Add(hideoutConfig);
                    totalCreated++;
                    LogHelper.Debug(
                        logger,
                        $"Registered hideout customization: {hideoutConfig.Id} ({hideoutConfig.Type} - {hideoutConfig.SystemName})"
                    );
                }
                catch (Exception ex)
                {
                    logger.Error(
                        $"Failed to register hideout customization {hideoutConfig.Id}: {ex.Message}"
                    );
                }
            }
        }

        return totalCreated;
    }

    private int LoadAndRegisterHideoutIcons(string hideoutIconsDir)
    {
        var iconsRegistered = 0;
        string[] imageExtensions = [".png", ".jpg", ".jpeg", ".bmp"];

        try
        {
            foreach (var iconPath in Directory.GetFiles(hideoutIconsDir))
            {
                var ext = Path.GetExtension(iconPath).ToLowerInvariant();
                if (imageExtensions.Contains(ext))
                {
                    var iconName = Path.GetFileNameWithoutExtension(iconPath);
                    _hideoutIconPaths[iconName] = iconPath;
                    LogHelper.Debug(logger, $"Registered hideout icon: {iconName}");
                    iconsRegistered++;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading hideout icons: {ex.Message}");
        }

        return iconsRegistered;
    }

    private int LoadAndRegisterMarkTextures(string markTexturesDir)
    {
        var texturesRegistered = 0;
        string[] imageExtensions = [".png", ".jpg", ".jpeg", ".bmp"];

        try
        {
            foreach (var texturePath in Directory.GetFiles(markTexturesDir))
            {
                var ext = Path.GetExtension(texturePath).ToLowerInvariant();
                if (imageExtensions.Contains(ext))
                {
                    var textureName = Path.GetFileNameWithoutExtension(texturePath);
                    _markTexturePaths[textureName] = texturePath;
                    LogHelper.Debug(logger, $"Registered mark texture: {textureName}");
                    texturesRegistered++;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading mark textures: {ex.Message}");
        }

        return texturesRegistered;
    }

    public List<string> GetHideoutIconManifest()
    {
        return _hideoutIconPaths.Keys.ToList();
    }

    public string GetHideoutIconFullPath(string iconName)
    {
        if (_hideoutIconPaths.TryGetValue(iconName, out var path))
        {
            // Convert absolute path to relative path from SPT root
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;
            var relativePath = Path.GetRelativePath(rootPath, path);
            return relativePath;
        }
        return null;
    }

    public async Task<byte[]?> GetHideoutIconData(string iconName)
    {
        if (_hideoutIconPaths.TryGetValue(iconName, out var path) && File.Exists(path))
            return await File.ReadAllBytesAsync(path);
        return null;
    }

    public List<string> GetMarkTextureManifest()
    {
        return _markTexturePaths.Keys.ToList();
    }

    public async Task<byte[]?> GetMarkTextureData(string textureName)
    {
        if (_markTexturePaths.TryGetValue(textureName, out var path) && File.Exists(path))
            return await File.ReadAllBytesAsync(path);
        return null;
    }

    public string GetMarkTextureFullPath(string textureName)
    {
        if (_markTexturePaths.TryGetValue(textureName, out var path))
        {
            // Convert absolute path to relative path from SPT root
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;
            var relativePath = Path.GetRelativePath(rootPath, path);
            return relativePath;
        }
        return null;
    }
}
