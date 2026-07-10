using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using WTTClientCommonLib.Configuration;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;

namespace WTTClientCommonLib.Services;

public static class ZoneService
{
    private static readonly List<CustomZoneContainer> Zones = new();
    private static int _currentSelectIndex = -1;
    private static string _selectedZoneName = "No CustomQuestZone Selected";

    public static void AddExistingZones()
    {
        ZoneConfigManager.ExistingQuestZones.ForEach(questZone =>
        {
            var cube = Helpers.Utils.CreateNewZoneCube(questZone.ZoneName);
            if (cube == null)
                return;

            var position = new Vector3(
                float.Parse(questZone.Position.X),
                float.Parse(questZone.Position.Y),
                float.Parse(questZone.Position.Z)
            );
            var scale = new Vector3(
                float.Parse(questZone.Scale.X),
                float.Parse(questZone.Scale.Y),
                float.Parse(questZone.Scale.Z)
            );
            var rotation = new Quaternion(
                float.Parse(questZone.Rotation.X),
                float.Parse(questZone.Rotation.Y),
                float.Parse(questZone.Rotation.Z),
                float.Parse(questZone.Rotation.W)
            );

            cube.transform.position = position;
            cube.transform.rotation = rotation;
            cube.transform.localScale = scale;

            var customZoneContainer = new CustomZoneContainer(
                cube,
                questZone.ZoneType,
                questZone.FlareType
            );
            Zones.Add(customZoneContainer);
        });
        ZoneConfigManager.ExistingQuestZones.Clear();
    }

    public static void CreateNewZone()
    {
        var name = ZoneConfigManager.NewZoneName.Value;
        if (string.IsNullOrWhiteSpace(name) || Zones.Exists(z => z.GameObject.name == name))
            return;

        var type = ZoneConfigManager.NewZoneType.Value;
        var flare = string.IsNullOrEmpty(ZoneConfigManager.FlareZoneType.Value)
            ? ""
            : ZoneConfigManager.FlareZoneType.Value;
        var obj = Helpers.Utils.CreateNewZoneCube(name);
        if (obj == null)
            return;

        Zones.Add(new CustomZoneContainer(obj, type, flare));
        LogHelper.LogDebug($"Created new zone: {name}, total zones: {Zones.Count}");
    }

    public static void NextZone()
    {
        if (Zones.Count < 1)
            return;

        if (_currentSelectIndex >= 0)
            Zones[_currentSelectIndex].GameObject.GetComponent<Renderer>().material.color =
                ZoneConfigManager.ColorZoneRed;

        if (_currentSelectIndex + 1 < Zones.Count)
            _currentSelectIndex++;
        else
            _currentSelectIndex = 0;

        Zones[_currentSelectIndex].GameObject.GetComponent<Renderer>().material.color =
            ZoneConfigManager.ColorZoneGreen;
        _selectedZoneName = Zones[_currentSelectIndex].GameObject.name;
        AdjustConfigValues();
    }

    public static void PrevZone()
    {
        if (Zones.Count < 1)
            return;

        if (_currentSelectIndex >= 0)
            Zones[_currentSelectIndex].GameObject.GetComponent<Renderer>().material.color =
                ZoneConfigManager.ColorZoneRed;

        if (_currentSelectIndex - 1 < 0)
            _currentSelectIndex = Zones.Count - 1;
        else
            _currentSelectIndex--;

        Zones[_currentSelectIndex].GameObject.GetComponent<Renderer>().material.color =
            ZoneConfigManager.ColorZoneGreen;
        _selectedZoneName = Zones[_currentSelectIndex].GameObject.name;
        AdjustConfigValues();
    }

    public static string GetCurrentZoneName()
    {
        return _selectedZoneName;
    }

    public static void AdjustPosition()
    {
        if (Zones.Count < 1 || _currentSelectIndex < 0)
            return;

        var currentZone = Zones[_currentSelectIndex].GameObject;
        var newPosition = new Vector3(
            ZoneConfigManager.PositionConfigX.Value,
            ZoneConfigManager.PositionConfigY.Value,
            ZoneConfigManager.PositionConfigZ.Value
        );
        currentZone.transform.position = newPosition;
    }

    public static void AdjustScale()
    {
        if (Zones.Count < 1 || _currentSelectIndex < 0)
            return;

        var currentZone = Zones[_currentSelectIndex].GameObject;
        var newScale = new Vector3(
            ZoneConfigManager.ScaleConfigX.Value,
            ZoneConfigManager.ScaleConfigY.Value,
            ZoneConfigManager.ScaleConfigZ.Value
        );
        currentZone.transform.localScale = newScale;
    }

    public static void AdjustRotation()
    {
        if (Zones.Count < 1 || _currentSelectIndex < 0)
            return;

        var currentZone = Zones[_currentSelectIndex].GameObject;
        var newRotation = Quaternion.Euler(
            ZoneConfigManager.RotationConfigX.Value,
            ZoneConfigManager.RotationConfigY.Value,
            ZoneConfigManager.RotationConfigZ.Value
        );
        currentZone.transform.rotation = newRotation;
    }

    public static void OutputZones()
    {
        if (Zones.Count < 1)
            return;

        var outputDir = Path.GetFullPath(
            Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\")
        );
        var path = Path.Combine(
            outputDir,
            $"WTT-ClientCommonLib-CustomQuestZone-Output-{DateTime.Now:yyyyMMddHHmmssffff}.json"
        );

        using (var streamWriter = File.CreateText(path))
        {
            var convertedZones = Helpers.Utils.ConvertZoneFormat(
                Zones,
                Helpers.Utils.GetLocationId()
            );
            var serializer = new JsonSerializer { Formatting = Formatting.Indented };
            serializer.Serialize(streamWriter, convertedZones);
        }

        LogHelper.LogDebug($"WTT-ClientCommonLib: Output zones to file: {path}");
    }

    private static void AdjustConfigValues()
    {
        if (Zones.Count < 1 || _currentSelectIndex < 0)
            return;

        var currentZone = Zones[_currentSelectIndex].GameObject;

        ZoneConfigManager.PositionConfigX.Value = currentZone.transform.position.x;
        ZoneConfigManager.PositionConfigY.Value = currentZone.transform.position.y;
        ZoneConfigManager.PositionConfigZ.Value = currentZone.transform.position.z;

        ZoneConfigManager.ScaleConfigX.Value = currentZone.transform.localScale.x;
        ZoneConfigManager.ScaleConfigY.Value = currentZone.transform.localScale.y;
        ZoneConfigManager.ScaleConfigZ.Value = currentZone.transform.localScale.z;

        var rotationEulerAngles = currentZone.transform.rotation.eulerAngles;
        ZoneConfigManager.RotationConfigX.Value = rotationEulerAngles.x;
        ZoneConfigManager.RotationConfigY.Value = rotationEulerAngles.y;
        ZoneConfigManager.RotationConfigZ.Value = rotationEulerAngles.z;
    }
}
