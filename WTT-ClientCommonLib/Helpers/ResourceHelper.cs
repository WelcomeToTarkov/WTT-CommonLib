using System;
using EFT.Utilities;

namespace WTTClientCommonLib.Helpers;

public static class ResourceHelper
{
    public static void AddEntry(string key, object value)
    {
        if (!ResourcesCache.dictionary_0.ContainsKey(key))
        {
            ResourcesCache.dictionary_0.Add(key, value);
            LogHelper.LogDebug($"[WTT-ClientCommonLib] Registered {key}.");
        }
        else
        {
            LogHelper.LogDebug($"[WTT-ClientCommonLib] Duplicate key ignored: {key}");
        }
    }
}
