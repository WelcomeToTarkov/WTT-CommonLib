using System;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;
using WTTClientCommonLib.Components;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;

namespace WTTClientCommonLib.Services;

internal class QuestZones
{
    public static List<CustomQuestZone> GetZones()
    {
        var request = Utils.Get<List<CustomQuestZone>>("/wttcommonlib/zones/get");
        if (request == null || request.Count == 0)
        {
            ConsoleScreen.Log("[QuestZones.GetZones] No zones data loaded.");
            return new List<CustomQuestZone>();
        }

        foreach (var zone in request)
        {
            zone.Position.W ??= "0";
            zone.Rotation.W ??= "0";
            zone.Scale.W ??= "0";
        }
#if DEBUG
        var loadedZoneCount = 0;
        if (request != null)
            foreach (var zone in request)
                if (zone.ZoneLocation.ToLower() == Singleton<GameWorld>.Instance.MainPlayer.Location.ToLower())
                {
                    ConsoleScreen.Log("-------------------------------------");
                    ConsoleScreen.Log("ZoneScale:");
                    ConsoleScreen.Log($"Scale Z: {zone.Scale.Z}");
                    ConsoleScreen.Log($"Scale Y: {zone.Scale.Y}");
                    ConsoleScreen.Log($"Scale X: {zone.Scale.X}");
                    ConsoleScreen.Log("ZonePosition:");
                    ConsoleScreen.Log($"Position Z: {zone.Position.Z}");
                    ConsoleScreen.Log($"Position Y: {zone.Position.Y}");
                    ConsoleScreen.Log($"Position X: {zone.Position.X}");
                    ConsoleScreen.Log("ZoneRotation:");
                    ConsoleScreen.Log($"Rotation Z: {zone.Rotation.Z}");
                    ConsoleScreen.Log($"Rotation Y: {zone.Rotation.Y}");
                    ConsoleScreen.Log($"Rotation X: {zone.Rotation.X}");
                    ConsoleScreen.Log($"Rotation W: {zone.Rotation.W}");
                    ConsoleScreen.Log($"ZoneType: {zone.ZoneType}");
                    if (!string.IsNullOrEmpty(zone.FlareType)) ConsoleScreen.Log($"FlareType: {zone.FlareType}");
                    else ConsoleScreen.Log("FlareType: N/A");
                    ConsoleScreen.Log($"ZoneLocation: {zone.ZoneLocation}");
                    ConsoleScreen.Log($"ZoneId: {zone.ZoneId}");
                    ConsoleScreen.Log($"ZoneName: {zone.ZoneName}");
                    ConsoleScreen.Log("-------------------------------------");
                    loadedZoneCount++;
                }

        ConsoleScreen.Log("-------------------------------------");
        ConsoleScreen.Log($"Loaded CustomQuestZone Count: {loadedZoneCount}");
        ConsoleScreen.Log($"Player Map Location: {Singleton<GameWorld>.Instance.MainPlayer.Location}");
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
        foreach (var zone in zones)
        {
            if (zone.ZoneType.ToLower() == "placeitem") ZoneCreateItem(zone);

            if (zone.ZoneType.ToLower() == "visit") ZoneCreateVisit(zone);

            if (zone.ZoneType.ToLower() == "flarezone") ZoneCreateFlareZone(zone);

            if (zone.ZoneType.ToLower() == "botkillzone") ZoneCreateBotKillZone(zone);
        }
    }
}