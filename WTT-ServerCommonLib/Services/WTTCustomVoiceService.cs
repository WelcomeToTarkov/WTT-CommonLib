using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostLoad)]
public class WTTCustomVoiceService(
    ISptLogger<WTTCustomVoiceService> logger,
    TemplateTable templateTable,
    BotTable botTable,
    LocaleTable localeTable,
    ConfigHelper configHelper,
    ModHelper modHelper
)
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, string> _voiceBundleMappings = [];

    /// <summary>
    /// Loads custom voice configs from JSON/JSONC files and registers them to the game database.
    ///
    /// Voices are loaded from the mod's "db/CustomVoices" directory (or a custom path if specified).
    ///
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomVoices(Assembly assembly, string? relativePath = null)
    {
        try
        {
            var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            var defaultDir = Path.Combine("db", "CustomVoices");
            var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

            if (!Directory.Exists(finalDir))
            {
                logger.Warning($"Voices directory not found at {finalDir}");
                return;
            }

            var voiceConfigDicts = await configHelper.LoadAllJsonFiles<
                Dictionary<string, CustomVoiceConfig>
            >(finalDir);

            if (voiceConfigDicts.Count == 0)
            {
                logger.Warning($"No valid custom voice configs found in {finalDir}");
                return;
            }

            var totalVoicesCreated = 0;

            foreach (var dict in voiceConfigDicts)
            {
                if (dict.Count == 0)
                    continue;

                foreach (var (voiceId, config) in dict)
                    if (ProcessVoiceConfig(voiceId, config))
                    {
                        if (!string.IsNullOrEmpty(config.BundlePath))
                            RegisterVoiceBundle(config.Name, config.BundlePath);
                        totalVoicesCreated++;
                    }
            }

            LogHelper.Debug(
                logger,
                $"Created {totalVoicesCreated} custom voices from {voiceConfigDicts.Count} files"
            );
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading voice configs: {ex.Message}");
        }
    }

    private bool ProcessVoiceConfig(string voiceId, CustomVoiceConfig voiceConfig)
    {
        try
        {
            CreateAndAddVoice(voiceId, voiceConfig);
            AddVoiceToCustomizationStorage(voiceId, voiceConfig);
            HandleLocale(voiceId, voiceConfig);
            ProcessBotVoices(voiceId, voiceConfig);

            LogHelper.Debug(logger, $"Created custom voice {voiceId}");
            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to create voice {voiceId}: {ex.Message}");
            return false;
        }
    }

    private void CreateAndAddVoice(string voiceId, CustomVoiceConfig voiceConfig)
    {
        var voice = new CustomizationItem
        {
            Id = voiceId,
            Name = voiceConfig.Name,
            Parent = "5fc100cf95572123ae738483",
            Type = "Item",
            Properties = new CustomizationProperties
            {
                Name = voiceConfig.Name,
                ShortName = voiceConfig.Name,
                Description = voiceConfig.Name,
                Side = voiceConfig.SideSpecificVoice ?? ["Usec", "Bear"],
                Prefab = voiceConfig.Name,
            },
        };

        templateTable.Customization[voiceId] = voice;
        LogHelper.Debug(logger, $"Added voice customization: {voiceId}");

        if (voiceConfig.AddVoiceToPlayer)
        {
            templateTable.Character.Add(voiceId);
            LogHelper.Debug(logger, $"Added voice {voiceId} to player character");
        }
    }

    private void AddVoiceToCustomizationStorage(string voiceId, CustomVoiceConfig voiceConfig)
    {
        if (!voiceConfig.AddVoiceToPlayer)
            return;

        var customizationStorage = templateTable.CustomisationStorage;

        var voiceStorage = new CustomisationStorage
        {
            Id = voiceId,
            Source = CustomisationSource.DEFAULT,
            Type = CustomisationType.VOICE,
        };

        customizationStorage.Add(voiceStorage);
    }

    private void HandleLocale(string voiceId, CustomVoiceConfig voiceConfig)
    {
        if (voiceConfig.Locales == null)
            return;

        var globalLocales = localeTable.Global;
        var voiceLocaleKey = $"{voiceId} Name";

        foreach (var (localeCode, lazyLocale) in globalLocales)
            lazyLocale.AddTransformer(localeData =>
            {
                if (localeData == null)
                    return localeData;

                if (voiceConfig.Locales.TryGetValue(localeCode, out var localizedName))
                    localeData[voiceLocaleKey] = localizedName;
                else if (voiceConfig.Locales.TryGetValue("en", out var fallbackName))
                    localeData[voiceLocaleKey] = fallbackName;

                return localeData;
            });
    }

    private void ProcessBotVoices(string voiceId, CustomVoiceConfig voiceConfig)
    {
        if (voiceConfig.AddToBotTypes == null)
            return;

        foreach (var (botType, weight) in voiceConfig.AddToBotTypes)
            try
            {
                var botTypeKey = botType.ToLower();

                if (!botTable.Types.TryGetValue(botTypeKey, out var botDb))
                {
                    logger.Warning($"Bot type '{botTypeKey}' not found in database");
                    continue;
                }

                if (botDb != null)
                    botDb.BotAppearance.Voice[voiceId] = weight;

                LogHelper.Debug(
                    logger,
                    $"Added voice {voiceId} to bot type '{botTypeKey}' with weight {weight}"
                );
            }
            catch (Exception ex)
            {
                logger.Error($"Error adding voice {voiceId} to bot type '{botType}': {ex.Message}");
            }
    }

    private void RegisterVoiceBundle(string voiceId, string bundlePath)
    {
        lock (_lock)
        {
            if (_voiceBundleMappings.TryAdd(voiceId, bundlePath))
                LogHelper.Debug(logger, $"Registered voice bundle: {voiceId} -> {bundlePath}");
            else
                logger.Warning($"Voice bundle {voiceId} already registered");
        }
    }

    public Dictionary<string, string> GetVoiceBundleMappings()
    {
        lock (_lock)
        {
            return _voiceBundleMappings;
        }
    }
}
