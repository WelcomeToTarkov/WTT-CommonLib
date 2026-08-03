using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Helpers;
using WTTServerCommonLib.Models;
using Path = System.IO.Path;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomClothingService(
    ISptLogger<WTTCustomClothingService> logger,
    TemplateTable templateTable,
    TradersTable tradersTable,
    LocaleTable localeTable,
    ModHelper modHelper,
    ConfigHelper configHelper
)
{
    /// <summary>
    /// Loads custom clothing configs from JSON/JSONC files and registers them to the game database.
    ///
    /// Clothing is loaded from the mod's "db/CustomClothing" directory (or a custom path if specified).
    /// Supports both tops (with body and hands) and bottoms that can be sold by traders.
    /// </summary>
    /// <param name="assembly">The calling assembly, used to determine the mod folder location</param>
    /// <param name="relativePath">(OPTIONAL) Custom path relative to the mod folder</param>
    public async Task CreateCustomClothing(Assembly assembly, string? relativePath = null)
    {
        try
        {
            var assemblyLocation = modHelper.GetAbsolutePathToModFolder(assembly);
            var defaultDir = Path.Combine("db", "CustomClothing");
            var finalDir = Path.Combine(assemblyLocation, relativePath ?? defaultDir);

            if (!Directory.Exists(finalDir))
            {
                logger.Warning($"Clothing directory not found at {finalDir}");
                return;
            }

            var clothingConfigsList = await configHelper.LoadAllJsonFiles<
                List<CustomClothingConfig>
            >(finalDir);

            if (clothingConfigsList.Count == 0)
            {
                logger.Warning($"No valid clothing configs found in {finalDir}");
                return;
            }

            var totalClothingCreated = 0;
            foreach (var configList in clothingConfigsList)
            {
                foreach (var config in configList)
                {
                    try
                    {
                        config.Validate();
                        if (ProcessClothingConfig(config))
                            totalClothingCreated++;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.Error($"Config validation failed: {ex.Message}");
                    }
                }
            }

            LogHelper.Debug(
                logger,
                $"Created {totalClothingCreated} custom clothing items from {clothingConfigsList.Count} files"
            );
        }
        catch (Exception ex)
        {
            logger.Error($"Error loading clothing configs: {ex.Message}");
        }
    }

    private bool ProcessClothingConfig(CustomClothingConfig config)
    {
        try
        {
            return config.Type?.ToLower() switch
            {
                "top" => AddTop(config),
                "bottom" => AddBottom(config),
                _ => throw new InvalidOperationException($"Unknown clothing type: {config.Type}"),
            };
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to process clothing config: {ex.Message}");
            return false;
        }
    }

    private bool AddTop(CustomClothingConfig config)
    {
        try
        {
            // Create top customization item
            var topItem = new CustomizationItem
            {
                Id = config.TopId!,
                Name = $"{config.TopId}_name",
                Parent = "5cc0868e14c02e000c6bea68",
                Type = "Item",
                Properties = new CustomizationProperties
                {
                    Description = $"{config.TopId}_desc",
                    Name = $"{config.TopId}_name",
                    ShortName = $"{config.TopId}_shortName",
                    Side = config.Side ?? ["Usec", "Bear"],
                    BodyPart = "Body",
                    IntegratedArmorVest = false,
                    Prefab = new Prefab { Path = config.TopBundlePath!, Rcid = "" },
                    WatchPosition = config.WatchPosition ?? new Vector3(0, 0, 0),
                    WatchPrefab = config.WatchPrefab ?? new Prefab { Path = "", Rcid = "" },
                    WatchRotation = config.WatchRotation ?? new Vector3(0, 0, 0),
                },
                Prototype = "5cde95d97d6c8b647a3769b0",
            };

            templateTable.Customization[config.TopId!] = topItem;
            LogHelper.Debug(logger, $"Added top customization: {config.TopId}");

            // Create hands customization item
            var handsItem = new CustomizationItem
            {
                Id = config.HandsId!,
                Name = $"{config.HandsId}_name",
                Parent = "5cc086a314c02e000c6bea69",
                Type = "Item",
                Properties = new CustomizationProperties
                {
                    Description = $"{config.HandsId}_desc",
                    Name = $"{config.HandsId}_name",
                    ShortName = $"{config.HandsId}_shortName",
                    Side = config.Side ?? ["Usec", "Bear"],
                    BodyPart = "Hands",
                    IntegratedArmorVest = false,
                    Prefab = new Prefab { Path = config.HandsBundlePath!, Rcid = "" },
                    WatchPosition = config.WatchPosition ?? new Vector3(0, 0, 0),
                    WatchPrefab = config.WatchPrefab ?? new Prefab { Path = "", Rcid = "" },
                    WatchRotation = config.WatchRotation ?? new Vector3(0, 0, 0),
                },
                Prototype = "5cde95fa7d6c8b04737c2d13",
            };

            templateTable.Customization[config.HandsId!] = handsItem;
            LogHelper.Debug(logger, $"Added hands customization: {config.HandsId}");

            // Create suite
            var suite = new CustomizationItem
            {
                Id = config.SuiteId!,
                Name = $"{config.SuiteId}_name",
                Parent = "5cd944ca1388ce03a44dc2a4",
                Type = "Item",
                Properties = new CustomizationProperties
                {
                    Description = "DefaultUsecUpperSuite",
                    Name = "DefaultUsecUpperSuite",
                    ShortName = "DefaultUsecUpperSuite",
                    Side = config.Side ?? ["Usec", "Bear", "Savage"],
                    AvailableAsDefault = false,
                    Game = ["eft", "arena"],
                    Body = config.TopId!,
                    Hands = config.HandsId!,
                },
                Prototype = "5cde9ec17d6c8b04723cf479",
            };

            templateTable.Customization[config.SuiteId!] = suite;
            LogHelper.Debug(logger, $"Added suite customization: {config.SuiteId}");

            HandleLocale(config, config.SuiteId!);
            AddSuiteToTrader(config);

            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to add top for outfitId {config.OutfitId}: {ex.Message}");
            return false;
        }
    }

    private bool AddBottom(CustomClothingConfig config)
    {
        try
        {
            // Create bottom customization item
            var bottomItem = new CustomizationItem
            {
                Id = config.BottomId!,
                Name = $"{config.BottomId}_name",
                Parent = "5cc0869814c02e000a4cad94",
                Type = "Item",
                Properties = new CustomizationProperties
                {
                    Description = $"{config.BottomId}_desc",
                    Name = $"{config.BottomId}_name",
                    ShortName = $"{config.BottomId}_shortName",
                    Side = config.Side ?? ["Usec", "Bear", "Savage"],
                    BodyPart = "Feet",
                    IntegratedArmorVest = false,
                    Prefab = new Prefab { Path = config.BottomBundlePath!, Rcid = "" },
                    WatchPosition = new Vector3(0, 0, 0),
                    WatchPrefab = new Prefab { Path = "", Rcid = "" },
                    WatchRotation = new Vector3(0, 0, 0),
                },
                Prototype = "5cdea3c47d6c8b0475341734",
            };

            templateTable.Customization[config.BottomId!] = bottomItem;
            LogHelper.Debug(logger, $"Added bottom customization: {config.BottomId}");

            // Create suite
            var suite = new CustomizationItem
            {
                Id = config.SuiteId!,
                Name = $"{config.SuiteId}_name",
                Parent = "5cd944d01388ce000a659df9",
                Type = "Item",
                Properties = new CustomizationProperties
                {
                    Description = $"{config.SuiteId}_desc",
                    Name = $"{config.SuiteId}_name",
                    ShortName = $"{config.SuiteId}_shortName",
                    Side = config.Side ?? ["Usec", "Bear", "Savage"],
                    AvailableAsDefault = false,
                    Game = ["eft", "arena"],
                    Feet = config.BottomId!,
                },
                Prototype = "5cd946231388ce000d572fe3",
            };

            templateTable.Customization[config.SuiteId!] = suite;
            LogHelper.Debug(logger, $"Added suite customization: {config.SuiteId}");

            HandleLocale(config, config.SuiteId!);
            AddSuiteToTrader(config);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to add bottom for outfitId {config.OutfitId}: {ex.Message}");
            return false;
        }
    }

    private void AddSuiteToTrader(CustomClothingConfig config)
    {
        MongoId actualTraderId;

        if (TraderIds.TraderMap.TryGetValue(config.TraderId.ToLower(), out var traderId))
        {
            actualTraderId = traderId;
        }
        else if (config.TraderId.IsValidMongoId())
        {
            actualTraderId = config.TraderId;
        }
        else
        {
            logger.Error($"Invalid trader key: {config.TraderId}");
            return;
        }

        tradersTable[actualTraderId].Base.CustomizationSeller = true;
        tradersTable[actualTraderId].Suits ??= [];

        var itemRequirements = new List<ItemRequirement>();
        string currencyId = ItemTplResolver.ResolveId(config.CurrencyId);
        itemRequirements.Add(
            new ItemRequirement
            {
                Id = null,
                Type = "ItemRequirement",
                Count = config.Price,
                Tpl = currencyId,
                OnlyFunctional = true,
            }
        );

        var traderSuit = new Suit
        {
            Id = config.OutfitId!,
            Tid = actualTraderId,
            SuiteId = config.SuiteId!,
            IsActive = true,
            IsHiddenInPVE = false,
            ExternalObtain = false,
            InternalObtain = true,
            Requirements = new SuitRequirements
            {
                LoyaltyLevel = config.LoyaltyLevel,
                ProfileLevel = config.ProfileLevel,
                Standing = config.Standing,
                QuestRequirements = config.QuestRequirements ?? [],
                AchievementRequirements = config.AchievementRequirements ?? [],
                ItemRequirements = itemRequirements,
                RequiredTid = actualTraderId,
            },
        };

        tradersTable[actualTraderId].Suits?.Add(traderSuit);
        LogHelper.Debug(logger, $"Added suite {config.SuiteId} to trader {config.TraderId}");
    }

    private void HandleLocale(CustomClothingConfig config, string clothingId)
    {
        if (config.Locales == null)
            return;

        var globalLocales = localeTable.Global;

        foreach (var (localeCode, lazyLocale) in globalLocales)
            lazyLocale.AddTransformer(localeData =>
            {
                if (localeData == null)
                    return localeData;

                var localeInfo =
                    config.Locales.GetValueOrDefault(localeCode)
                    ?? config.Locales.GetValueOrDefault("en");

                if (localeInfo != null)
                {
                    var itemKey = clothingId;
                    var nameKey = $"{clothingId} Name";
                    var descriptionKey = $"{clothingId} Description";

                    var nameValue = localeInfo.Name ?? "";
                    var descriptionValue = localeInfo.Description ?? "";

                    localeData[itemKey] = nameValue;
                    localeData[nameKey] = nameValue;
                    localeData[descriptionKey] = descriptionValue;
                }

                return localeData;
            });
    }
}
