using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Vector3 = System.Numerics.Vector3;

namespace WTTServerCommonLib.Models;

public class CustomClothingConfig
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("suiteId")]
    public required string SuiteId { get; set; }

    [JsonPropertyName("outfitId")]
    public required string OutfitId { get; set; }

    [JsonPropertyName("topId")]
    public string? TopId { get; set; }

    [JsonPropertyName("handsId")]
    public string? HandsId { get; set; }

    [JsonPropertyName("bottomId")]
    public string? BottomId { get; set; }

    [JsonPropertyName("side")]
    public List<string>? Side { get; set; }

    [JsonPropertyName("locales")]
    public Dictionary<string, LocaleDetails> Locales { get; set; }

    [JsonPropertyName("topBundlePath")]
    public string? TopBundlePath { get; set; }

    [JsonPropertyName("handsBundlePath")]
    public string? HandsBundlePath { get; set; }

    [JsonPropertyName("bottomBundlePath")]
    public string? BottomBundlePath { get; set; }

    [JsonPropertyName("traderId")]
    public required string TraderId { get; set; } = string.Empty;

    [JsonPropertyName("loyaltyLevel")]
    public int? LoyaltyLevel { get; set; }

    [JsonPropertyName("profileLevel")]
    public int? ProfileLevel { get; set; }

    [JsonPropertyName("standing")]
    public double? Standing { get; set; }

    [JsonPropertyName("currencyId")]
    public required string CurrencyId { get; set; }

    [JsonPropertyName("price")]
    public required int Price { get; set; }

    [JsonPropertyName("watchPrefab")]
    public Prefab? WatchPrefab { get; set; }

    [JsonPropertyName("watchPosition")]
    public SPTarkov.Server.Core.Models.Eft.Common.Vector3? WatchPosition { get; set; }

    [JsonPropertyName("watchRotation")]
    public SPTarkov.Server.Core.Models.Eft.Common.Vector3? WatchRotation { get; set; }

    [JsonPropertyName("questRequirements")]
    public List<string>? QuestRequirements { get; set; }

    [JsonPropertyName("achievementRequirements")]
    public List<string>? AchievementRequirements { get; set; }
}

public static class CustomClothingConfigValidator
{
    /// <summary>
    /// Validates the clothing config for required properties and logical consistency.
    /// Throws an InvalidOperationException with descriptive error messages if validation fails.
    /// </summary>
    public static void Validate(this CustomClothingConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        ValidateRequiredFields(config);
        ValidateTypeSpecific(config);
        ValidatePaymentMethod(config);
        ValidateLocales(config);
        ValidateTraderAndRequirements(config);
    }

    private static void ValidateRequiredFields(CustomClothingConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Type))
            throw new InvalidOperationException(
                "'type' is required and cannot be empty. Use 'top' or 'bottom'."
            );

        if (string.IsNullOrWhiteSpace(config.SuiteId))
            throw new InvalidOperationException("'suiteId' is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(config.OutfitId))
            throw new InvalidOperationException("'outfitId' is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(config.TraderId))
            throw new InvalidOperationException("'traderId' is required and cannot be empty.");

        var typeValue = config.Type.ToLower();
        if (typeValue != "top" && typeValue != "bottom")
            throw new InvalidOperationException(
                $"'type' must be 'top' or 'bottom', got '{config.Type}'."
            );
    }

    private static void ValidateTypeSpecific(CustomClothingConfig config)
    {
        var typeValue = config.Type.ToLower();

        if (typeValue == "top")
        {
            if (string.IsNullOrWhiteSpace(config.TopId))
                throw new InvalidOperationException("For type 'top', 'topId' is required.");

            if (string.IsNullOrWhiteSpace(config.TopBundlePath))
                throw new InvalidOperationException("For type 'top', 'topBundlePath' is required.");

            if (string.IsNullOrWhiteSpace(config.HandsId))
                throw new InvalidOperationException("For type 'top', 'handsId' is required.");

            if (string.IsNullOrWhiteSpace(config.HandsBundlePath))
                throw new InvalidOperationException(
                    "For type 'top', 'handsBundlePath' is required."
                );

            if (config.BottomId != null || config.BottomBundlePath != null)
                throw new InvalidOperationException(
                    "For type 'top', 'bottomId' and 'bottomBundlePath' should not be defined."
                );
        }
        else if (typeValue == "bottom")
        {
            if (string.IsNullOrWhiteSpace(config.BottomId))
                throw new InvalidOperationException("For type 'bottom', 'bottomId' is required.");

            if (string.IsNullOrWhiteSpace(config.BottomBundlePath))
                throw new InvalidOperationException(
                    "For type 'bottom', 'bottomBundlePath' is required."
                );

            if (
                config.TopId != null
                || config.TopBundlePath != null
                || config.HandsId != null
                || config.HandsBundlePath != null
            )
                throw new InvalidOperationException(
                    "For type 'bottom', 'topId', 'topBundlePath', 'handsId', and 'handsBundlePath' should not be defined."
                );
        }
    }

    private static void ValidatePaymentMethod(CustomClothingConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.CurrencyId))
            throw new InvalidOperationException(
                "If 'price' is defined, 'currencyId' is also required."
            );

        if (config.Price <= 0)
            throw new InvalidOperationException(
                "'price' must be present and be a positive number."
            );
    }

    private static void ValidateLocales(CustomClothingConfig config)
    {
        if (config.Locales != null && config.Locales.Count > 0)
        {
            foreach (var (locale, details) in config.Locales)
            {
                if (details == null)
                    throw new InvalidOperationException($"Locale '{locale}' has null details.");

                if (string.IsNullOrWhiteSpace(details.Name))
                    throw new InvalidOperationException($"Locale '{locale}': 'name' is required.");
                if (string.IsNullOrWhiteSpace(details.Description))
                    throw new InvalidOperationException(
                        $"Locale '{locale}': 'description' is required."
                    );
            }

            if (!config.Locales.ContainsKey("en"))
                throw new InvalidOperationException(
                    "Locales must include 'en' as the fallback locale."
                );
        }
    }

    private static void ValidateTraderAndRequirements(CustomClothingConfig config)
    {
        if (config.LoyaltyLevel.HasValue && (config.LoyaltyLevel < 0 || config.LoyaltyLevel > 4))
            throw new InvalidOperationException(
                $"'loyaltyLevel' must be between 0 and 4, got {config.LoyaltyLevel}."
            );

        if (config.ProfileLevel.HasValue && config.ProfileLevel < 0)
            throw new InvalidOperationException(
                $"'profileLevel' must be non-negative, got {config.ProfileLevel}."
            );

        ValidateStringList(config.QuestRequirements, "questRequirements");
        ValidateStringList(config.AchievementRequirements, "achievementRequirements");
    }

    private static void ValidateStringList(IEnumerable<string>? list, string fieldName)
    {
        if (list?.Count() > 0)
        {
            var count = 0;
            foreach (var entry in list)
            {
                if (string.IsNullOrWhiteSpace(entry))
                    throw new InvalidOperationException($"{fieldName}[{count}] is empty or null.");

                count++;
            }
        }
    }
}
