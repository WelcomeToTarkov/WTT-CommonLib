using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomLocaleService(
    ISptLogger<WTTCustomLocaleService> logger,
    LocaleTable localeTable,
    ModHelper modHelper,
    ConfigHelper configHelper
)
{
    /// <summary>
    /// Loads custom locale translations from JSON/JSONC files and registers them globally.
    ///
    /// Locales are loaded from the mod's "db/CustomLocales" directory (or a custom path if specified).
    /// Translations are merged into all available game locales using English as fallback.
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomLocales(Assembly assembly, string? relativePath = null)
    {
        var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
        var defaultDir = Path.Combine("db", "CustomLocales");
        var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

        if (!Directory.Exists(finalDir))
        {
            logger.Warning($"Locale directory not found: {finalDir}");
            return;
        }

        var customLocales = await configHelper.LoadLocalesFromDirectory(finalDir);

        if (customLocales.Count == 0)
        {
            logger.Warning("No custom locale files found or loaded");
            return;
        }

        var fallback = customLocales.TryGetValue("en", out var locale)
            ? locale
            : customLocales.Values.FirstOrDefault();

        if (fallback == null)
        {
            logger.Warning("No valid fallback locale found");
            return;
        }

        foreach (var (localeCode, lazyLocale) in localeTable.Global)
            lazyLocale.AddTransformer(localeData =>
            {
                if (localeData is null)
                    return localeData;

                var customLocale = customLocales.GetValueOrDefault(localeCode, fallback);

                foreach (var (key, value) in customLocale)
                    localeData[key] = value;

                return localeData;
            });

        LogHelper.Debug(
            logger,
            $"WTTCustomLocaleService: Registered transformers for {customLocales.Count} locale files"
        );
    }
}
