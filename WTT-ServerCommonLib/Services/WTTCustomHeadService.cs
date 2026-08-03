using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomHeadService(
    ISptLogger<WTTCustomHeadService> logger,
    TemplateTable templateTable,
    LocaleTable localeTable,
    ModHelper modHelper,
    ConfigHelper configHelper
)
{
    /// <summary>
    /// Loads custom head configs from JSON/JSONC files and registers them to the game database.
    ///
    /// Heads are loaded from the mod's "db/CustomHeads" directory (or a custom path if specified).
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomHeads(Assembly assembly, string? relativePath = null)
    {
        try
        {
            var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            var defaultDir = Path.Combine("db", "CustomHeads");
            var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

            if (!Directory.Exists(finalDir))
            {
                logger.Warning($"Heads directory not found at {finalDir}");
                return;
            }

            var headConfigDicts = await configHelper.LoadAllJsonFiles<
                Dictionary<string, CustomHeadConfig>
            >(finalDir);

            if (headConfigDicts.Count == 0)
            {
                logger.Warning($"No valid head configs found in {finalDir}");
                return;
            }

            var totalHeadsCreated = 0;

            foreach (var configDict in headConfigDicts)
            {
                if (configDict.Count == 0)
                    continue;

                foreach (var (headId, customHeadConfig) in configDict)
                    if (ProcessCustomHeadConfig(headId, customHeadConfig))
                        totalHeadsCreated++;
            }

            LogHelper.Debug(
                logger,
                $"Created {totalHeadsCreated} custom heads from {headConfigDicts.Count} files"
            );
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading head configs: {ex.Message}");
        }
    }

    private bool ProcessCustomHeadConfig(string headId, CustomHeadConfig customHeadConfig)
    {
        try
        {
            var customizationItem = GenerateHeadCustomizationItem(headId, customHeadConfig);

            AddHeadToTemplates(headId, customizationItem, customHeadConfig.AddHeadToPlayer);
            AddHeadToCustomizationStorage(headId, customHeadConfig.AddHeadToPlayer);
            AddHeadLocales(headId, customHeadConfig);

            LogHelper.Debug(logger, $"Created custom head {headId}");
            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to create head {headId}: {ex.Message}");
            return false;
        }
    }

    private CustomizationItem GenerateHeadCustomizationItem(
        string headId,
        CustomHeadConfig customHeadConfig
    )
    {
        return new CustomizationItem
        {
            Id = headId,
            Name = "",
            Parent = "5cc085e214c02e000c6bea67",
            Type = "Item",
            Properties = new CustomizationProperties
            {
                AvailableAsDefault = customHeadConfig.AvailableAsDefault ?? true,
                Name = "",
                ShortName = "",
                Description = "",
                Side = customHeadConfig.Side,
                BodyPart = "Head",
                IntegratedArmorVest = false,
                ProfileVersions = [],
                Prefab = new Prefab { Path = customHeadConfig.Path, Rcid = "" },
                WatchPrefab = new Prefab { Path = "", Rcid = "" },
                WatchPosition = new Vector3
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                },
                WatchRotation = new Vector3
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                },
                Game = [],
                Body = "",
                Hands = "",
                Feet = "",
            },
            Prototype = "5cc2e4d014c02e000d0115f8",
        };
    }

    private void AddHeadToCustomizationStorage(string headId, bool addHeadToPlayer)
    {
        if (!addHeadToPlayer)
            return;

        var customizationStorage = templateTable.CustomisationStorage;

        var headStorage = new CustomisationStorage
        {
            Id = headId,
            Source = CustomisationSource.DEFAULT,
            Type = CustomisationType.HEAD,
        };

        customizationStorage.Add(headStorage);
    }

    private void AddHeadToTemplates(
        string headId,
        CustomizationItem customizationItem,
        bool addHeadToPlayer
    )
    {
        templateTable.Customization[headId] = customizationItem;

        if (addHeadToPlayer)
            templateTable.Character.Add(headId);
    }

    private void AddHeadLocales(string headId, CustomHeadConfig customHeadConfig)
    {
        if (customHeadConfig.Locales == null)
            return;

        var globalLocales = localeTable.Global;
        var headLocaleKey = $"{headId} Name";

        foreach (var (localeCode, lazyLocale) in globalLocales)
            lazyLocale.AddTransformer(localeData =>
            {
                if (localeData == null)
                    return localeData;

                if (customHeadConfig.Locales.TryGetValue(localeCode, out var localizedName))
                    localeData[headLocaleKey] = localizedName;
                else if (customHeadConfig.Locales.TryGetValue("en", out var fallbackName))
                    localeData[headLocaleKey] = fallbackName;

                return localeData;
            });
    }
}
