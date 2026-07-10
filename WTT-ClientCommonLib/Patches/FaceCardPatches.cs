using System.Reflection;
using Arena.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using WTTClientCommonLib.Components;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Patches;

internal class FaceCardViewInitPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(FaceCardView), nameof(FaceCardView.Init));
    }

    [PatchPostfix]
    static void Postfix(FaceCardView __instance, string faceName)
    {
        var existingHandler = __instance.gameObject.GetComponent<CharacterAudioHandler>();
        if (existingHandler != null)
        {
            LogHelper.LogDebug($"Handler already exists for {faceName}, skipping duplicate");
            return;
        }

        var audioKey = ResourceLoader.GetAudioForFace(faceName);
        if (string.IsNullOrEmpty(audioKey))
        {
            LogHelper.LogDebug($"No audio found for face: {faceName}");
            return;
        }

        if (!ResourceLoader.ClipCache.TryGetAudioClip(audioKey, out var audioClip))
        {
            LogHelper.LogDebug($"Audio clip not in cache: {audioKey}");
            return;
        }

        LogHelper.LogDebug($"Creating audio handler for {faceName} with audio {audioKey}");

        var audioHandler = __instance.gameObject.AddComponent<CharacterAudioHandler>();
        audioHandler.Initialize(faceName); // Only pass faceName
    }
}

internal class FaceCardViewTogglePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(FaceCardView), nameof(FaceCardView.SetSelected));
    }

    [PatchPostfix]
    private static void Postfix(FaceCardView __instance, bool isSelected)
    {
        var handler = __instance.gameObject.GetComponent<CharacterAudioHandler>();

        if (handler == null)
        {
            LogHelper.LogDebug($"No CharacterAudioHandler found on {__instance.gameObject.name}");
            return;
        }

        LogHelper.LogDebug(
            $"FaceCard toggle: {__instance.gameObject.name}, Selected: {isSelected}"
        );

        if (isSelected)
        {
            LogHelper.LogDebug("Fading in audio...");
            handler.FadeIn();
        }
        else
        {
            LogHelper.LogDebug("Fading out audio...");
            handler.FadeOut();
        }
    }
}
