using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers;
using WTTServerCommonLib.Helpers;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomAchievementService(
    ISptLogger<WTTCustomAchievementService> logger,
    ImageRouter imageRouter,
    ModHelper modHelper,
    ConfigHelper configHelper,
    TemplateTable templateTable,
    LocaleTable localeTable
)
{
    private readonly string[] _validImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    /// <summary>
    /// Loads custom achievements, locales, and images.
    ///
    /// Achievements are loaded from the mod's "db/CustomAchievements/Achievements" directory.
    /// Locales are loaded from the mod's "db/CustomAchievements/Locales" directory.
    /// Images are loaded from the mod's "db/CustomAchievements/Images" directory.
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomAchievements(Assembly assembly, string? relativePath = null)
    {
        var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        var defaultDir = Path.Combine("db", "CustomAchievements");
        var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

        await LoadAllCustomAchievements(finalDir);
    }

    private async Task LoadAllCustomAchievements(string basePath)
    {
        if (!Directory.Exists(basePath))
        {
            logger.Warning($"Achievement base directory not found: {basePath}");
            return;
        }

        LogHelper.Debug(logger, $"Loading achievements from {basePath}");

        var achievementFiles = await LoadAchievementFiles(Path.Combine(basePath, "Achievements"));
        var imageFiles = LoadImageFiles(Path.Combine(basePath, "Images"));

        ImportAchievementData(achievementFiles);
        await ImportLocaleData(basePath);
        ImportImageData(imageFiles);
    }

    private async Task<List<List<Achievement>>> LoadAchievementFiles(string achievementsDir)
    {
        var result = new List<List<Achievement>>();

        if (!Directory.Exists(achievementsDir))
        {
            logger.Warning($"Achievements directory not found: {achievementsDir}");
            return result;
        }

        try
        {
            var achievementLists = await configHelper.LoadAllJsonFiles<List<Achievement>>(
                achievementsDir
            );

            foreach (var achievementData in achievementLists)
            {
                if (achievementData.Count != 0)
                {
                    result.Add(achievementData);
                    LogHelper.Debug(
                        logger,
                        $"Loaded achievement data with {achievementData.Count} achievements"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(
                $"Error scanning for achievement files in {achievementsDir}: {ex.Message}"
            );
        }

        return result;
    }

    private List<string> LoadImageFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            LogHelper.Debug(logger, $"Images directory not found: {directoryPath}");
            return new List<string>();
        }

        try
        {
            var images = Directory
                .GetFiles(directoryPath)
                .Where(f => _validImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            LogHelper.Debug(logger, $"Found {images.Count} image files in {directoryPath}");
            return images;
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading images from {directoryPath}: {ex.Message}");
            return new List<string>();
        }
    }

    private void ImportAchievementData(List<List<Achievement>> achievementFiles)
    {
        if (achievementFiles.Count == 0)
        {
            LogHelper.Debug(logger, "No achievement files found or loaded");
            return;
        }

        var achievementCount = 0;
        foreach (var file in achievementFiles)
        {
            foreach (var achievement in file)
            {
                var achievementId = achievement.Id;
                if (!achievementId.IsValidMongoId())
                {
                    logger.Warning($"Invalid achievement ID '{achievement.Id}', skipping");
                    continue;
                }

                templateTable.Achievements.Add(achievement);
                achievementCount++;
                LogHelper.Debug(logger, $"Added achievement {achievement.Id}");
            }
        }

        LogHelper.Debug(logger, $"Successfully loaded {achievementCount} achievements");
    }

    private async Task ImportLocaleData(string basePath)
    {
        var localesPath = Path.Combine(basePath, "Locales");

        try
        {
            var locales = await configHelper.LoadLocalesFromDirectory(localesPath);

            if (locales.Count == 0)
            {
                LogHelper.Debug(logger, $"No locale files found or loaded from {localesPath}");
                return;
            }

            var fallback = locales.TryGetValue("en", out var englishLocales)
                ? englishLocales
                : locales.Values.FirstOrDefault();

            if (fallback == null)
                return;

            foreach (var (localeCode, lazyLocale) in localeTable.Global)
            {
                lazyLocale.AddTransformer(localeData =>
                {
                    if (localeData is null)
                        return localeData;

                    var customLocale = locales.GetValueOrDefault(localeCode, fallback);

                    foreach (var (key, value) in customLocale)
                    {
                        localeData[key] = value;
                    }

                    return localeData;
                });
            }

            LogHelper.Debug(
                logger,
                $"Registered transformers for {locales.Count} achievement locale files"
            );
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading achievement locales: {ex.Message}");
        }
    }

    private void ImportImageData(List<string> imageFiles)
    {
        if (imageFiles.Count == 0)
        {
            LogHelper.Debug(logger, "No images found");
            return;
        }

        foreach (var imagePath in imageFiles)
        {
            try
            {
                var imageName = Path.GetFileNameWithoutExtension(imagePath);

                imageRouter.AddRoute($"/files/achievement/{imageName}", imagePath);
                LogHelper.Debug(logger, $"Registered image route for {imageName}");
            }
            catch (Exception ex)
            {
                logger.Warning($"Failed to register image {imagePath}: {ex.Message}");
            }
        }

        LogHelper.Debug(logger, $"Loaded {imageFiles.Count} images");
    }
}
