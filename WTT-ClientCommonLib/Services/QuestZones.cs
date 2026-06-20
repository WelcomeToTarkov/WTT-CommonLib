using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WTTClientCommonLib.Components;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;
using static SalvageItemTrigger;

namespace WTTClientCommonLib.Services;

internal class QuestZones
{
    private static readonly HashSet<string> _usedGroupPositions = new();
    private static readonly List<SalvageReward> _salvageRewards = new();
    private static string PosKey(ZoneTransform p) => $"{p.X}|{p.Y}|{p.Z}";

    public static List<CustomQuestZone> GetZones()
    {
        var request = Utils.Get<List<CustomQuestZone>>("/wttcommonlib/zones/get");
        if (request == null || request.Count == 0)
        {
            LogHelper.LogDebug("[QuestZones.GetZones] No zones data loaded.");
            return new List<CustomQuestZone>();
        }

        foreach (var zone in request)
        {
            if (zone.Position != null)
            {
                zone.Position.W ??= "0";
                zone.Rotation.W ??= "0";
                zone.Scale.W ??= "0";
            }
            else
            {
                if (zone.GroupPosition != null)
                {
                    foreach (var randomZone in zone.GroupPosition)
                    {
                        randomZone.Position.W ??= "0";
                        randomZone.Rotation.W ??= "0";
                        randomZone.Scale.W ??= "0";
                    }
                }
            }
        }
#if DEBUG
        var loadedZoneCount = 0;
        if (request != null)
            foreach (var zone in request)
                if (zone.ZoneLocation.ToLower() == Singleton<GameWorld>.Instance.MainPlayer.Location.ToLower())
                {
                    LogHelper.LogDebug("-------------------------------------");
                    LogHelper.LogDebug("ZoneScale:");
                    LogHelper.LogDebug($"Scale Z: {zone.Scale.Z}");
                    LogHelper.LogDebug($"Scale Y: {zone.Scale.Y}");
                    LogHelper.LogDebug($"Scale X: {zone.Scale.X}");
                    LogHelper.LogDebug("ZonePosition:");
                    LogHelper.LogDebug($"Position Z: {zone.Position.Z}");
                    LogHelper.LogDebug($"Position Y: {zone.Position.Y}");
                    LogHelper.LogDebug($"Position X: {zone.Position.X}");
                    LogHelper.LogDebug("ZoneRotation:");
                    LogHelper.LogDebug($"Rotation Z: {zone.Rotation.Z}");
                    LogHelper.LogDebug($"Rotation Y: {zone.Rotation.Y}");
                    LogHelper.LogDebug($"Rotation X: {zone.Rotation.X}");
                    LogHelper.LogDebug($"Rotation W: {zone.Rotation.W}");
                    LogHelper.LogDebug($"ZoneType: {zone.ZoneType}");
                    if (!string.IsNullOrEmpty(zone.FlareType)) LogHelper.LogDebug($"FlareType: {zone.FlareType}");
                    else LogHelper.LogDebug("FlareType: N/A");
                    LogHelper.LogDebug($"ZoneLocation: {zone.ZoneLocation}");
                    LogHelper.LogDebug($"ZoneId: {zone.ZoneId}");
                    LogHelper.LogDebug($"ZoneName: {zone.ZoneName}");
                    LogHelper.LogDebug("-------------------------------------");
                    loadedZoneCount++;
                }

        LogHelper.LogDebug("-------------------------------------");
        LogHelper.LogDebug($"Loaded CustomQuestZone Count: {loadedZoneCount}");
        LogHelper.LogDebug($"Player Map Location: {Singleton<GameWorld>.Instance.MainPlayer.Location}");
#endif
        return request;
    }

    private static void ZoneCreateItem(CustomQuestZone customQuestZone)
    {
        var newZone = new GameObject();

        var boxCollider = newZone.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        var position = new Vector3(float.Parse(customQuestZone.Position.X), float.Parse(customQuestZone.Position.Y),
            float.Parse(customQuestZone.Position.Z));
        var scale = new Vector3(float.Parse(customQuestZone.Scale.X), float.Parse(customQuestZone.Scale.Y),
            float.Parse(customQuestZone.Scale.Z));
        var rotation = new Quaternion(float.Parse(customQuestZone.Rotation.X), float.Parse(customQuestZone.Rotation.Y),
            float.Parse(customQuestZone.Rotation.Z), float.Parse(customQuestZone.Rotation.W));

        newZone.transform.position = position;
        newZone.transform.localScale = scale;
        newZone.transform.rotation = rotation;

        var trigger = newZone.AddComponent<PlaceItemTrigger>();
        trigger.SetId(customQuestZone.ZoneId);

        newZone.layer = LayerMask.NameToLayer("Triggers");
        newZone.name = customQuestZone.ZoneId;
    }

    private static void ZoneCreateVisit(CustomQuestZone customQuestZone)
    {
        var newZone = new GameObject();

        var boxCollider = newZone.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        var position = new Vector3(float.Parse(customQuestZone.Position.X), float.Parse(customQuestZone.Position.Y),
            float.Parse(customQuestZone.Position.Z));
        var scale = new Vector3(float.Parse(customQuestZone.Scale.X), float.Parse(customQuestZone.Scale.Y),
            float.Parse(customQuestZone.Scale.Z));
        var rotation = new Quaternion(float.Parse(customQuestZone.Rotation.X), float.Parse(customQuestZone.Rotation.Y),
            float.Parse(customQuestZone.Rotation.Z), float.Parse(customQuestZone.Rotation.W));

        newZone.transform.position = position;
        newZone.transform.localScale = scale;
        newZone.transform.rotation = rotation;

        var trigger = newZone.AddComponent<ExperienceTrigger>();
        trigger.SetId(customQuestZone.ZoneId);

        newZone.layer = LayerMask.NameToLayer("Triggers");
        newZone.name = customQuestZone.ZoneId;
    }

    private static void ZoneCreateBotKillZone(CustomQuestZone customQuestZone)
    {
        var newZone = new GameObject();

        var boxCollider = newZone.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        var position = new Vector3(float.Parse(customQuestZone.Position.X), float.Parse(customQuestZone.Position.Y),
            float.Parse(customQuestZone.Position.Z));
        var scale = new Vector3(float.Parse(customQuestZone.Scale.X), float.Parse(customQuestZone.Scale.Y),
            float.Parse(customQuestZone.Scale.Z));
        var rotation = new Quaternion(float.Parse(customQuestZone.Rotation.X), float.Parse(customQuestZone.Rotation.Y),
            float.Parse(customQuestZone.Rotation.Z), float.Parse(customQuestZone.Rotation.W));

        newZone.transform.position = position;
        newZone.transform.localScale = scale;
        newZone.transform.rotation = rotation;

        var trigger = newZone.AddComponent<TriggerWithId>();
        trigger.SetId(customQuestZone.ZoneId);

        newZone.layer = LayerMask.NameToLayer("Triggers");
        newZone.name = customQuestZone.ZoneId;
    }

    private static void ZoneCreateSalvage(CustomQuestZone customQuestZone)
    {
        var salvage = customQuestZone.Salvage;
        if (salvage == null)
        {
            LogHelper.LogWarn($"Salvage zone '{customQuestZone.ZoneId}' missing Salvage config");
            return;
        }

        var newZone = new GameObject();

        var boxCollider = newZone.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        var position = new Vector3(
            float.Parse(customQuestZone.Position.X),
            float.Parse(customQuestZone.Position.Y),
            float.Parse(customQuestZone.Position.Z));

        var scale = new Vector3(
            float.Parse(customQuestZone.Scale.X),
            float.Parse(customQuestZone.Scale.Y),
            float.Parse(customQuestZone.Scale.Z));

        var rotation = new Quaternion(
            float.Parse(customQuestZone.Rotation.X),
            float.Parse(customQuestZone.Rotation.Y),
            float.Parse(customQuestZone.Rotation.Z),
            float.Parse(customQuestZone.Rotation.W));

        newZone.transform.position = position;
        newZone.transform.localScale = scale;
        newZone.transform.rotation = rotation;

        var trigger = newZone.AddComponent<SalvageItemTrigger>();
        trigger.SetId(customQuestZone.ZoneId);


        var rewards = salvage.Rewards?.Select(r => new SalvageItemTrigger.SalvageReward
        {
            ItemTpl = r.ItemTpl,
            Count = r.Count,
            ToQuestInventory = r.ToQuestInventory
        }) ?? Array.Empty<SalvageItemTrigger.SalvageReward>();

        _salvageRewards.AddRange(rewards);

        trigger.Configure(
            salvage.RequiredItemTpl,
            salvage.SalvageTime,
            rewards,
            salvage.ConsumeRequiredItem 
        );

        newZone.layer = LayerMask.NameToLayer("Triggers");
        newZone.name = customQuestZone.ZoneId;
    }

    private static void ZoneCreateFlareZone(CustomQuestZone customQuestZone)
    {
        // Thank you Groovey :)
        var newZone = new GameObject();

        var boxCollider = newZone.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;


        var position = new Vector3(float.Parse(customQuestZone.Position.X), float.Parse(customQuestZone.Position.Y),
            float.Parse(customQuestZone.Position.Z));
        var scale = new Vector3(float.Parse(customQuestZone.Scale.X), float.Parse(customQuestZone.Scale.Y),
            float.Parse(customQuestZone.Scale.Z));
        var rotation = new Quaternion(float.Parse(customQuestZone.Rotation.X), float.Parse(customQuestZone.Rotation.Y),
            float.Parse(customQuestZone.Rotation.Z), float.Parse(customQuestZone.Rotation.W));

        newZone.transform.position = position;
        newZone.transform.localScale = scale;
        newZone.transform.rotation = rotation;

        var flareTrigger = newZone.AddComponent<ZoneFlareTrigger>();
        flareTrigger.SetId(customQuestZone.ZoneId);

        newZone.AddComponent<MoveObjectsToAdditionalPhysSceneMarker>();

        var flareDetector = newZone.AddComponent<FlareShootDetectorZone>();

        var flareDetectorType = typeof(FlareShootDetectorZone);
        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        var zoneIDField = flareDetectorType.GetField("zoneID", bindingFlags);
        if (zoneIDField != null) zoneIDField.SetValue(flareDetector, customQuestZone.ZoneId);

        var flareType = (FlareEventType)Enum.Parse(typeof(FlareEventType), customQuestZone.FlareType);
        var flareTypeForHandleField = flareDetectorType.GetField("flareTypeForHandle", bindingFlags);
        if (flareTypeForHandleField != null) flareTypeForHandleField.SetValue(flareDetector, flareType);

        var triggerHandler = newZone.AddComponent<PhysicsTriggerHandler>();
        triggerHandler.trigger = boxCollider;

        var triggerHandlersField = flareDetectorType.GetField("_triggerHandlers", bindingFlags);
        if (triggerHandlersField != null)
        {
            var triggerHandlers = (List<PhysicsTriggerHandler>)triggerHandlersField.GetValue(flareDetector);
            triggerHandlers.Add(triggerHandler);
        }

        newZone.layer = LayerMask.NameToLayer("Triggers");
        newZone.name = customQuestZone.ZoneId;
    }

    public static void CreateZones(List<CustomQuestZone> zones)
    {
        _usedGroupPositions.Clear();
        _salvageRewards.Clear();

        foreach (var zone in zones)
        {
            ApplyGroupPositionIfAny(zone);

            var type = zone.ZoneType.ToLowerInvariant();
            if (type == "placeitem") ZoneCreateItem(zone);
            if (type == "visit") ZoneCreateVisit(zone);
            if (type == "flarezone") ZoneCreateFlareZone(zone);
            if (type == "botkillzone") ZoneCreateBotKillZone(zone);
            if (type == "salvage") ZoneCreateSalvage(zone);
        }

        _ = PreloadSalvageRewards();
    }


    private static async Task PreloadSalvageRewards()
    {
        var poolManager = Singleton<PoolManagerClass>.Instance;
        if (poolManager == null)
            return;

        var itemFactory = Singleton<ItemFactoryClass>.Instance;
        if (itemFactory == null)
            return;

        var keys = new List<ResourceKey>();

        foreach (var reward in _salvageRewards)
        {
            if (!itemFactory.ItemTemplates.TryGetValue(reward.ItemTpl, out var template))
                continue;

            if (template.Prefab != null) keys.Add(template.Prefab);
            if (template.UsePrefab != null) keys.Add(template.UsePrefab);
        }

        if (keys.Count == 0)
            return;

        await poolManager.LoadBundlesAndCreatePools(
            0,
            PoolManagerClass.AssemblyType.Local,
            keys.ToArray(),
            JobPriorityClass.Immediate,
            null,
            CancellationToken.None
        );
    }

    private static void ApplyGroupPositionIfAny(CustomQuestZone zone)
    {
        if (zone.GroupPosition == null || zone.GroupPosition.Count == 0)
            return;

        var candidates = new List<int>();
        for (int i = 0; i < zone.GroupPosition.Count; i++)
        {
            var pos = zone.GroupPosition[i];
            var key = PosKey(pos.Position);
            if (!_usedGroupPositions.Contains(key))
                candidates.Add(i);
        }

        int index;

        if (candidates.Count > 0)
        {
            index = UnityEngine.Random.Range(0, candidates.Count);
            index = candidates[index];
        }
        else
        {
            LogHelper.LogWarn(
                $"All GroupPosition positions for {zone.ZoneId} are already used; allowing reuse.");
            index = UnityEngine.Random.Range(0, zone.GroupPosition.Count);
        }

        var selectedPose = zone.GroupPosition[index];

        zone.Position = selectedPose.Position;
        zone.Rotation = selectedPose.Rotation;
        zone.Scale = selectedPose.Scale;

        var usedKey = PosKey(selectedPose.Position);
        _usedGroupPositions.Add(usedKey);

#if DEBUG
        LogHelper.LogDebug(
            $"GroupPosition[{index}] selected for {zone.ZoneId} " +
            $"pos=({selectedPose.Position.X},{selectedPose.Position.Y},{selectedPose.Position.Z})");
#endif
    }
}