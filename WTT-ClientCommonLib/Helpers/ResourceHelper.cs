using EFT.Utilities;

namespace WTTClientCommonLib.Helpers;

public static class ResourceHelper
{
    public static void AddEntry(string key, object value)
    {
        if (!ResourcesCache._storage.ContainsKey(key))
        {
            ResourcesCache._storage.Add(key, value);
            LogHelper.LogDebug($"[WTT-ClientCommonLib] Registered {key}.");
        }
        else
        {
            LogHelper.LogDebug($"[WTT-ClientCommonLib] Duplicate key ignored: {key}");
        }
    }
}
