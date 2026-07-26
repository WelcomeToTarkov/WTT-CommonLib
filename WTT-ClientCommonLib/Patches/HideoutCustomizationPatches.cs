using System;
using System.Reflection;
using EFT;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Patches;

internal class HideoutCustomizationIconPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(HideoutCustomizationIcons),
            nameof(HideoutCustomizationIcons.GetSprite)
        );
    }

    [PatchPrefix]
    static bool Prefix(string id, ref Sprite __result, HideoutCustomizationIcons __instance)
    {
        if (ResourceLoader._customHideoutIcons.TryGetValue(id, out var customSprite))
        {
            LogHelper.LogDebug($"[HideoutIcons] Using custom sprite for: {id}");
            __result = customSprite;
            return false; // Skip original entirely
        }

        return true;
    }
}

internal class HideoutCustomizationTexturesPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(HideoutCustomizationController),
            nameof(HideoutCustomizationController.InstallCustomization)
        );
    }

    [PatchPrefix]
    static void Prefix(
        ResourceKey resourceKey,
        EHideoutCustomizationType customizationType,
        HideoutCustomizationController __instance
    )
    {
        if (customizationType != EHideoutCustomizationType.ShootingRangeMark)
            return;

        string assetName = resourceKey.ToAssetName();
        if (
            assetName == null
            || !ResourceLoader._customMarkTextures.TryGetValue(assetName, out var customTexture)
        )
            return;

        try
        {
            var icons = __instance._icons;
            if (icons != null)
            {
                icons.ShootingRangeMarkTextures.TryAdd(assetName, customTexture);
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"[HideoutTextures] Error injecting texture: {ex}");
        }
    }
}
