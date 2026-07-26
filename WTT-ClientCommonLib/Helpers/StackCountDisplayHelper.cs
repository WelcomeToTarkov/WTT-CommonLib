using System;

namespace WTTClientCommonLib.Helpers;

public static class StackCountDisplayHelper
{
    public static string GetShortBalance(int count, string itemTpl)
    {
        string result;
        switch (count)
        {
            case < 1000:
            {
                result = count.ToString();
                break;
            }
            case < 1000000:
            {
                var scaled = Math.Round(count / 1000.0, 1);
                if (scaled >= 1000) return "1.0M";
                result = scaled.ToString("0.0") + "K";
                break;
            }
            case < 1000000000:
            {
                var scaled = Math.Round(count / 1000000.0, 1);
                if (scaled >= 1000) return "1.0B";
                result = scaled.ToString("0.0") + "M";
                break;
            }
            default:
            {
                var scaledB = Math.Round(count / 1000000000.0, 1);
                result = scaledB.ToString("0.0") + "B";
                break;
            }
        }
        
        var currency = GetCurrencySymbol(itemTpl);
        
        return currency.Length <= 0 ? result : $"{result} {currency}";
    }

    private static string GetCurrencySymbol(string itemTpl)
    {
        return itemTpl switch
        {
            "5449016a4bdc2d6f028b456f" => "₽",
            "5696686a4bdc2da3298b456a" => "$",
            "569668774bdc2da2298b4568" => "€",
            _ => ""
        };
    }
}