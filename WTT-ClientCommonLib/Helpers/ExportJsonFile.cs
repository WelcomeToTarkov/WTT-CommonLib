using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Communications;
using Newtonsoft.Json;
using UnityEngine;
using WTTClientCommonLib.Configuration;

namespace WTTClientCommonLib.Helpers;

public static class ExportJsonFile
{
    public static JsonType JsonExportType;


    public enum JsonType
    {
        Zone,
        MapLocation,
        QuestLocation,
        ItemSpawnLocation
    }

    private static readonly string assemblyLocation = Assembly.GetExecutingAssembly().Location;
    internal static readonly string basePath = Path.GetFullPath(Path.Combine(assemblyLocation, @"..\..\..\..\"));
    private static string NewMongoId() => Guid.NewGuid().ToString("N").Substring(0, 24);

    public static void GenerateJson(JsonType type)
    {
        switch (type)
        {
            case JsonType.Zone: GenerateZoneJson(); break;
            case JsonType.MapLocation: GenerateCubeDataJson(); break;
            case JsonType.QuestLocation: GenerateQuestDataJson(); break;
            case JsonType.ItemSpawnLocation: GenerateItemSpawnJson(); break;
        }
    }

    private static void GenerateZoneJson()
    {
        if (Settings.CurrentZoneCubePosition.Value == Vector3.zero)
        {
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] You must generate a FetchLookPosition first!", ENotificationDurationType.Default,
                ENotificationIconType.Alert);
            return;
        }

        if (Settings.ZoneId.Value == "")
        {
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] You must set a Zone ID first!", ENotificationDurationType.Default,
                ENotificationIconType.Alert);
            return;
        }

        if (Settings.ZoneName.Value == "")
        {
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] You must set a Zone Name first!", ENotificationDurationType.Default,
                ENotificationIconType.Alert);
            return;
        }

        string filePath = Path.Combine(basePath, "BepInEx", "plugins", "WTT-ClientCommonLib", "Exports", "Zones", $"{Settings.ZoneId.Value.ToLower()}.json");
        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

        var zoneData = new[]
        {
            new
            {
                ZoneId = Settings.ZoneId.Value.ToLower().Replace(" ", "_"),
                ZoneName = Settings.ZoneName.Value.ToLower().Replace(" ", "_"),
                ZoneLocation = Singleton<GameWorld>.Instance.MainPlayer.Location,
                ZoneType = Settings.ZoneType.Value.ToString(),
                FlareType = GetFlareType(),
                Position = new
                {
                    X = $"{Settings.CurrentZoneCubePosition.Value.x}",
                    Y = $"{Settings.CurrentZoneCubePosition.Value.y}",
                    Z = $"{Settings.CurrentZoneCubePosition.Value.z}",
                    W = "0"
                },
                Rotation = new
                {
                    X = $"{Settings.CurrentZoneCubeRotation.Value.x}",
                    Y = $"{Settings.CurrentZoneCubeRotation.Value.y}",
                    Z = $"{Settings.CurrentZoneCubeRotation.Value.z}",
                    W = $"{Settings.CurrentZoneCubeRotation.Value.w}"
                },
                Scale = new
                {
                    X = $"{Settings.CurrentZoneCubeScale.Value.x}",
                    Y = $"{Settings.CurrentZoneCubeScale.Value.y}",
                    Z = $"{Settings.CurrentZoneCubeScale.Value.z}",
                    W = "0"
                }
            }
        };

        ZoneHelper.WriteIndented(filePath, zoneData);
        WTTClientCommonLib.Logger.LogMessage($"[WTT-ClientCommonLib] Quest Zone JSON file generated at {filePath} for zone {Settings.ZoneId.Value}");
        NotificationManagerClass.DisplayMessageNotification($"[WTT-ClientCommonLib] Quest Zone JSON file generated at {filePath} for zone {Settings.ZoneId.Value}");
    }

    private static string GetFlareType()
    {
        if (Settings.FlareType.Value == EFlareTypes.none) return "";
        return Settings.FlareType.Value.ToString();
    }

    public static void GenerateCubeDataJson()
    {
        if (Settings.CurrentMapName.Value == "")
        {
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] You must be in a map to export map positions.", ENotificationDurationType.Default,
                ENotificationIconType.Alert);
            return;
        }

        if (Settings.CubeDataList.Count == 0)
        {
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] No Map Positions to export.", ENotificationDurationType.Default,
                ENotificationIconType.Alert);
            return;
        }

        string mapName = Settings.CurrentMapName.Value;
        string readableMapName = Settings.MapIdToNameMap.ContainsKey(mapName)
            ? Settings.MapIdToNameMap[mapName]
            : "UnknownMap";

        string filePath = Path.Combine(basePath, "BepInEx", "plugins", "WTT-ClientCommonLib", "Exports", "MapLocations.json");
        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath!);

        var dataToExport = new Dictionary<string, List<object>>();

        if (File.Exists(filePath))
        {
            string existingData = File.ReadAllText(filePath);
            dataToExport = JsonConvert.DeserializeObject<Dictionary<string, List<object>>>(existingData)
                           ?? new Dictionary<string, List<object>>();
        }

        if (!dataToExport.ContainsKey(readableMapName))
            dataToExport[readableMapName] = new List<object>();

        var newData = Settings.CubeDataList.Select(location => new
        {
            Position = new { x = location.Position.x, y = location.Position.y, z = location.Position.z },
            Rotation = new { x = location.Rotation.x, y = location.Rotation.y, z = location.Rotation.z }
        }).ToList();

        dataToExport[readableMapName].AddRange(newData);

        ZoneHelper.WriteIndented(filePath, dataToExport);
        Settings.CubeDataList.Clear();

        WTTClientCommonLib.Logger.LogMessage($"[WTT-ClientCommonLib] Map Positions exported to {filePath}.");
        NotificationManagerClass.DisplayMessageNotification($"[WTT-ClientCommonLib] Map Positions exported to {filePath}.");
    }

    public static void GenerateQuestDataJson()
    {
        if (string.IsNullOrEmpty(Settings.CurrentMapName.Value))
        {
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] You must be in a map to export quest locations.",
                ENotificationDurationType.Default, ENotificationIconType.Alert);
            return;
        }

        if (Settings.CubeDataList == null || Settings.CubeDataList.Count == 0)
        {
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] No Quest Locations to export.", ENotificationDurationType.Default,
                ENotificationIconType.Alert);
            return;
        }

        string fileName = string.IsNullOrWhiteSpace(Settings.QuestLocationFileName.Value) ? "QuestLocations" : Settings.QuestLocationFileName.Value;
        string filePath = Path.Combine(basePath, "BepInEx", "plugins", "WTT-ClientCommonLib", "Exports", $"{fileName}.json");
        var dir = Path.GetDirectoryName(filePath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        List<object> questList = File.Exists(filePath)
            ? (JsonConvert.DeserializeObject<List<object>>(File.ReadAllText(filePath)) ?? new List<object>())
            : new List<object>();

        var pos = Settings.CurrentZoneCubePosition.Value;
        var eul = Settings.CurrentZoneCubeRotation.Value.eulerAngles;
        var isGroup = Settings.CubeDataList.Count > 1;

        var groupPositions = isGroup
            ? Settings.CubeDataList.Select((loc, i) => new
            {
                Name = $"groupPoint[{i}]",
                Weight = 0.5,
                Position = new { x = loc.Position.x, y = loc.Position.y, z = loc.Position.z },
                Rotation = new { x = loc.Rotation.x, y = loc.Rotation.y, z = loc.Rotation.z }
            }).Cast<object>().ToList()
            : new List<object>();

        var newQuest = new
        {
            locationId = string.IsNullOrWhiteSpace(Settings.QuestLocationId?.Value) ? NewMongoId() : Settings.QuestLocationId.Value,
            probability = 1.0,
            template = new
            {
                Id = NewMongoId(),
                IsContainer = false,
                useGravity = false,
                randomRotation = false,
                Position = new { x = pos.x, y = pos.y, z = pos.z },
                Rotation = new { x = eul.x, y = eul.y, z = eul.z },
                IsGroupPosition = isGroup,
                GroupPositions = groupPositions,
                IsAlwaysSpawn = true,
                Root = NewMongoId(),
                Items = new[]
                {
                    new
                    {
                        _id = NewMongoId(),
                        _tpl = Settings.QuestItemTPL.Value,
                        upd = new { StackObjectsCount = 1 }
                    }
                },
                itemDistribution = new
                {
                    composedKey = new { key = Settings.QuestItemTPL.Value }
                }
            }
        };

        questList.Add(newQuest);
        ZoneHelper.WriteIndented(filePath, questList);
        Settings.CubeDataList.Clear();

        WTTClientCommonLib.Logger.LogMessage($"[WTT-ClientCommonLib] Quest Locations exported to {filePath}.");
        NotificationManagerClass.DisplayMessageNotification($"[WTT-ClientCommonLib] Quest Locations exported to {filePath}.");
    }

    public static void GenerateItemSpawnJson()
    {
        if (string.IsNullOrEmpty(Settings.CurrentMapName.Value))
        {
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] You must be in a map to export item spawns.",
                ENotificationDurationType.Default, ENotificationIconType.Alert);
            return;
        }

        if (Settings.CubeDataList == null || Settings.CubeDataList.Count == 0)
        {
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] No Item Spawn locations to export.",
                ENotificationDurationType.Default, ENotificationIconType.Alert);
            return;
        }

        var filePath = Path.Combine(basePath, "BepInEx", "plugins", "WTT-ClientCommonLib", "Exports", $"{Settings.CurrentMapName.Value}_spawns.json");
        var dir = Path.GetDirectoryName(filePath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        Dictionary<string, List<object>> root;
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            root = JsonConvert.DeserializeObject<Dictionary<string, List<object>>>(json) ?? new Dictionary<string, List<object>>();
        }
        else root = new Dictionary<string, List<object>>();

        var mapKey = Settings.CurrentMapName.Value;
        if (!root.TryGetValue(mapKey, out var list) || list == null)
        {
            list = new List<object>();
            root[mapKey] = list;
        }

        var pos = Settings.CurrentZoneCubePosition.Value;
        var eul = Settings.CurrentZoneCubeRotation.Value.eulerAngles;
        var isGroup = Settings.CubeDataList.Count > 1;

        var groupPositions = isGroup
            ? Settings.CubeDataList.Select((loc, i) => new
            {
                Name = $"groupPoint[{i}]",
                Weight = 0.5,
                Position = new { x = loc.Position.x, y = loc.Position.y, z = loc.Position.z },
                Rotation = new { x = loc.Rotation.x, y = loc.Rotation.y, z = loc.Rotation.z }
            }).Cast<object>().ToList()
            : new List<object>();

        var spawnId = NewMongoId();
        var rootId = NewMongoId();
        var itemId = NewMongoId();
        var compId = NewMongoId();

        var entry = new
        {
            locationId = $"{spawnId}_loc",
            probability = Settings.ItemSpawnProbability.Value,
            template = new
            {
                Id = spawnId,
                IsContainer = Settings.ItemSpawnIsContainer.Value,
                useGravity = Settings.ItemSpawnGravity.Value,
                randomRotation = Settings.ItemSpawnRotation.Value,
                Position = new { x = pos.x, y = pos.y, z = pos.z },
                Rotation = new { x = eul.x, y = eul.y, z = eul.z },
                IsGroupPosition = isGroup,
                GroupPositions = groupPositions,
                IsAlwaysSpawn = Settings.ItemSpawnIsAlwaysSpawn.Value,
                Root = rootId,
                Items = new[]
                {
                    new
                    {
                        _id = itemId,
                        _tpl = Settings.QuestItemTPL.Value,
                        upd = new { StackObjectsCount = 1 }
                    }
                }
            },
            itemDistribution = new[]
            {
                new
                {
                    composedKey = new { key = compId },
                    relativeProbability = 1
                }
            }
        };

        list.Add(entry);
        ZoneHelper.WriteIndented(filePath, root);
        Settings.CubeDataList.Clear();

        WTTClientCommonLib.Logger.LogMessage($"[WTT-ClientCommonLib] Item Spawns exported to {filePath}.");
        NotificationManagerClass.DisplayMessageNotification($"[WTT-ClientCommonLib] Item Spawns exported to {filePath}.");
    }
}