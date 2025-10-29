using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using WTTClientCommonLib.Components;
using WTTClientCommonLib.Configuration;
using WTTClientCommonLib.Services;

namespace WTTClientCommonLib.Patches;

internal class OnGameStarted : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.Instance);
    }

    [PatchPostfix]
    private static void PatchPostfix(GameWorld __instance)
    {
        try
        {
            var currentMap = __instance.MainPlayer.Location;
            var questZones = QuestZones.GetZones();
            if (questZones == null || questZones.Count == 0)
            {
                Logger.LogDebug("No zones data loaded; skipping initialization.");
                return;
            }

            var validZones = questZones.Where(zone => zone.ZoneLocation.ToLower() == currentMap.ToLower()).ToList();
            Settings.ExistingQuestZones = validZones;
            
            ZoneCreatorComponent.Enable();
        
            Settings.CurrentMapName.Value = Singleton<GameWorld>.Instance.MainPlayer.Location;
            QuestZones.CreateZones(validZones);

            var player = __instance.MainPlayer;
            var locationID = __instance.LocationId;
            if (player?.Profile?.QuestsData == null) return;
            if (locationID == null) return;

            var loader = WTTClientCommonLib.Instance.AssetLoader;
            var configs = loader.SpawnConfigs;
            foreach (var config in configs)
                loader.ProcessSpawnConfig(__instance.MainPlayer, config, __instance.LocationId);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }
}