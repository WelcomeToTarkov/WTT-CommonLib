using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using System.Text.Json.Serialization;

public abstract class CustomItemConfigBase
{
    [JsonPropertyName("itemTplToClone")]
    public required string ItemTplToClone { get; set; }

    [JsonPropertyName("parentId")]
    public required string ParentId { get; set; }

    [JsonPropertyName("overrideProperties")]
    public required TemplateItemProperties OverrideProperties { get; set; }

    [JsonPropertyName("locales")]
    public required Dictionary<string, LocaleDetails> Locales { get; set; }

    [JsonPropertyName("addtoInventorySlots")]
    public List<string>? AddToInventorySlots { get; set; }

    [JsonPropertyName("addtoModSlots")]
    public bool? AddToModSlots { get; set; }

    [JsonPropertyName("modSlot")]
    public List<string>? ModSlot { get; set; }

    [JsonPropertyName("addtoSpecialSlots")]
    public bool? AddToSpecialSlots { get; set; }

    [JsonPropertyName("addtoSecureFilters")]
    public bool? AddToSecureFilters { get; set; }

    [JsonPropertyName("addtoItemBlacklist")]
    public bool? AddToItemBlacklist { get; set; }

    [JsonPropertyName("registerInHandbook")]
    public bool? RegisterInHandbook { get; set; }

    [JsonPropertyName("registerInFleaPrices")]
    public bool? RegisterInFleaPrices { get; set; }

    [JsonPropertyName("handbookParentId")]
    public string? HandbookParentId { get; set; }

    [JsonPropertyName("handbookPriceRoubles")]
    public int? HandbookPriceRoubles { get; set; }

    [JsonPropertyName("fleaPriceRoubles")]
    public int? FleaPriceRoubles { get; set; }

    public virtual IEnumerable<string> GetValidationErrors(string itemId)
    {
        if (!itemId.IsValidMongoId())
            yield return $"[{itemId}] is not a valid MongoId";

        if (string.IsNullOrWhiteSpace(ItemTplToClone))
            yield return $"[{itemId}] itemTplToClone is required";

        if (string.IsNullOrWhiteSpace(ParentId))
            yield return $"[{itemId}] parentId is required";

        if (OverrideProperties == null)
            yield return $"[{itemId}] overrideProperties is required";

        if (Locales == null || Locales.Count == 0)
            yield return $"[{itemId}] locales is required and must contain at least one locale";

        if (AddToInventorySlots != null)
        {
            for (var i = 0; i < AddToInventorySlots.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(AddToInventorySlots[i]))
                    yield return $"[{itemId}] addtoInventorySlots[{i}] must be a non-empty string";
            }
        }

        if (AddToModSlots == true)
        {
            if (ModSlot == null || ModSlot.Count == 0)
            {
                yield return $"[{itemId}] modSlot is required when addtoModSlots is true";
            }
            else
            {
                for (var i = 0; i < ModSlot.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(ModSlot[i]))
                        yield return $"[{itemId}] modSlot[{i}] must be a non-empty string";
                }
            }
        }

        var registerInHandbook = RegisterInHandbook ?? false;
        if (registerInHandbook)
        {
            if (string.IsNullOrWhiteSpace(HandbookParentId))
                yield return $"[{itemId}] handbookParentId is required when registerInHandbook is true";

            if (HandbookPriceRoubles == null || HandbookPriceRoubles < 0)
                yield return $"[{itemId}] handbookPriceRoubles must be >= 0 when registerInHandbook is true";
        }

        var registerInFleaPrices = RegisterInFleaPrices ?? false;
        if (registerInFleaPrices)
        {
            if (FleaPriceRoubles == null || FleaPriceRoubles < 0)
                yield return $"[{itemId}] fleaPriceRoubles must be >= 0 when registerInFleaPrices is true";
        }
    }
}