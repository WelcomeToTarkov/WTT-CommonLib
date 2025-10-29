using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using EFT.Communications;
using Newtonsoft.Json;
using UnityEngine;
using WTTClientCommonLib.Attributes;
using WTTClientCommonLib.Helpers;
using WTTClientCommonLib.Models;

namespace WTTClientCommonLib.Configuration;

internal class Settings
{
    private static readonly List<ConfigEntryBase> ConfigEntries = [];
    public static List<Location> CubeDataList = new List<Location>();
    private static readonly string basePath = AppDomain.CurrentDomain.BaseDirectory;
    public static List<CustomQuestZone> ExistingQuestZones { get; set; } = [];
    internal static Color ColorZoneRed = new(1f, 0f, 0f, 0.7f);
    internal static Color ColorZoneGreen = new(0f, 1f, 0f, 0.7f);

    #region Categories

    private const string ZoneInformation = "1. Zone Information";
    private const string ZoneBoxSettings = "2. Zone Box Settings";
    private const string Hotkeys = "3. Hotkeys";
    private const string QuestZoneSettings = "4. Quest Zone Settings";
    private const string MapLocations = "5. Map Locations";
    private const string QuestLocations = "6. Quest Locations";
    private const string ItemSpawnLocations = "7. Item Spawn Locations";

    #endregion

    #region 1. Zone Information

    public static ConfigEntry<Vector3> CurrentZoneCubePosition { get; set; }
    public static ConfigEntry<Quaternion> CurrentZoneCubeRotation { get; set; }
    public static ConfigEntry<Vector3> CurrentZoneCubeScale { get; set; }
    public static ConfigEntry<string> CurrentMapName { get; set; }

    public static ConfigEntry<float> ZoneCubeTransparency { get; set; }

    #endregion

    #region 2. Hotkeys

    public static ConfigEntry<KeyboardShortcut> ZoneCube { get; set; }
    public static ConfigEntry<KeyboardShortcut> ZonePrefabToggle { get; set; }
    public static ConfigEntry<KeyboardShortcut> RemoveZoneCube { get; set; }
    public static ConfigEntry<KeyboardShortcut> PositiveXKey { get; set; }
    public static ConfigEntry<KeyboardShortcut> NegativeXKey { get; set; }
    public static ConfigEntry<KeyboardShortcut> PositiveYKey { get; set; }
    public static ConfigEntry<KeyboardShortcut> NegativeYKey { get; set; }
    public static ConfigEntry<KeyboardShortcut> PositiveZKey { get; set; }
    public static ConfigEntry<KeyboardShortcut> NegativeZKey { get; set; }
    public static ConfigEntry<KeyboardShortcut> PositionModeKey { get; set; }
    public static ConfigEntry<KeyboardShortcut> ScaleModeKey { get; set; }
    public static ConfigEntry<KeyboardShortcut> RotateModeKey { get; set; }
    public static ConfigEntry<KeyboardShortcut> IncreaseTransformSpeed { get; set; }
    public static ConfigEntry<KeyboardShortcut> DecreaseTransformSpeed { get; set; }
    public static ConfigEntry<KeyboardShortcut> AddMapLocationToListKey { get; set; }
    public static ConfigEntry<KeyboardShortcut> RemoveMapLocationFromListKey { get; set; }

    #endregion

    #region 2. Zone Box Settings

    public static ConfigEntry<string> ZoneCubePrefab { get; set; }
    public static ConfigEntry<float> TransformSpeed { get; set; }
    public static ConfigEntry<bool> SpawnZoneCubeAtLookingPosition { get; set; }
    public static ConfigEntry<bool> LockXAndZRotation { get; set; }
    public static ConfigEntry<Vector3> DefaultScale { get; set; }
    public static ConfigEntry<float> PositionOffSet { get; set; }
    public static ConfigEntry<bool> ClearZoneCubeDataListOnGenerate { get; set; }

    #endregion

    #region 3. Quest Zone Settings

    public static ConfigEntry<string> ZoneId { get; set; }
    public static ConfigEntry<string> ZoneName { get; set; }
    public static ConfigEntry<EZoneTypes> ZoneType { get; set; }
    public static ConfigEntry<EFlareTypes> FlareType { get; set; }

    #endregion

    #region 4. Quest Locations

    public static ConfigEntry<string> QuestLocationFileName { get; set; }
    public static ConfigEntry<string> QuestLocationId { get; set; }
    public static ConfigEntry<string> QuestItemTPL { get; set; }

    #endregion

    #region 5. Item Spawn Locations

    public static ConfigEntry<double> ItemSpawnProbability { get; set; }
    public static ConfigEntry<bool> ItemSpawnIsContainer { get; set; }
    public static ConfigEntry<bool> ItemSpawnGravity { get; set; }
    public static ConfigEntry<bool> ItemSpawnRotation { get; set; }
    public static ConfigEntry<bool> ItemSpawnIsAlwaysSpawn { get; set; }

    #endregion

    public static void Init(ConfigFile config)
    {
        #region Config 1. Zone Information

        ConfigEntries.Add(CurrentZoneCubePosition = config.Bind(ZoneInformation, "Current Zone Cube Position", Vector3.zero, new ConfigDescription(
            "The current position of the Zone Cube.", null, new ConfigurationManagerAttributes { ReadOnly = true })));

        ConfigEntries.Add(CurrentZoneCubeRotation = config.Bind(ZoneInformation, "Current Zone Cube Rotation", Quaternion.identity, new ConfigDescription(
            "The current rotation of the Zone Cube.", null, new ConfigurationManagerAttributes { ReadOnly = true })));

        ConfigEntries.Add(CurrentZoneCubeScale = config.Bind(ZoneInformation, "Current Zone Cube Scale", Vector3.zero, new ConfigDescription(
            "The current scale of the Zone Cube.", null, new ConfigurationManagerAttributes { ReadOnly = true })));

        ConfigEntries.Add(CurrentMapName = config.Bind(ZoneInformation, "Current Map Name", "", new ConfigDescription(
            "The ID of the current map.", null, new ConfigurationManagerAttributes { ReadOnly = true })));

        ConfigEntries.Add(ZoneCubeTransparency = config.Bind(ZoneInformation, "Zone Cube Transparency", 0.5f, new ConfigDescription(
            "Transparency of the look position cube.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { })));

        #endregion

        #region Config 2. Hotkeys

        ConfigEntries.Add(AddMapLocationToListKey = config.Bind(Hotkeys, "Add Map Location to List", new KeyboardShortcut(KeyCode.UpArrow),
            new ConfigDescription(
                "Hotkey to add the current Zone Cube location to a list.", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(RemoveMapLocationFromListKey = config.Bind(Hotkeys, "Remove Last Map Location from List",
            new KeyboardShortcut(KeyCode.DownArrow), new ConfigDescription(
                "Hotkey to remove the last Zone Cube location from the list.", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(ZoneCube = config.Bind(Hotkeys, "Generate Zone Cube", new KeyboardShortcut(KeyCode.Keypad0),
            new ConfigDescription(
                "Fetches the position you are looking at and generates a Zone Cube.", null, new ConfigurationManagerAttributes { }, true)));

        ConfigEntries.Add(ZonePrefabToggle = config.Bind(Hotkeys, "Toggles Cube and Prefab", new KeyboardShortcut(KeyCode.KeypadPeriod),
            new ConfigDescription(
                "If you use a custom Bundle instead of default Cube you can toggle between them. (Resets scale to default bundle size)", null,
                new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(RemoveZoneCube = config.Bind(Hotkeys, "Remove Zone Cube", new KeyboardShortcut(KeyCode.KeypadEnter),
            new ConfigDescription(
                "Removes the look position Zone Cube.", null, new ConfigurationManagerAttributes { }, true)));

        ConfigEntries.Add(PositiveXKey = config.Bind(Hotkeys, "Transform Positive X", new KeyboardShortcut(KeyCode.Keypad1), new ConfigDescription(
            "Change Cube on the Positive X axis", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(NegativeXKey = config.Bind(Hotkeys, "Transform Negative X", new KeyboardShortcut(KeyCode.Keypad4), new ConfigDescription(
            "Change Cube on the Negative X axis", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(PositiveYKey = config.Bind(Hotkeys, "Transform Positive Y", new KeyboardShortcut(KeyCode.Keypad2), new ConfigDescription(
            "Change Cube on the Positive Y axis", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(NegativeYKey = config.Bind(Hotkeys, "Transform Negative Y", new KeyboardShortcut(KeyCode.Keypad5), new ConfigDescription(
            "Change Cube on the Negative Y axis", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(PositiveZKey = config.Bind(Hotkeys, "Transform Positive Z", new KeyboardShortcut(KeyCode.Keypad3), new ConfigDescription(
            "Change Cube on the Positive Z axis", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(NegativeZKey = config.Bind(Hotkeys, "Transform Negative Z", new KeyboardShortcut(KeyCode.Keypad6), new ConfigDescription(
            "Change Cube on the Negative Z axis", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(IncreaseTransformSpeed = config.Bind(Hotkeys, "Increase Transform Speed", new KeyboardShortcut(KeyCode.KeypadPlus),
            new ConfigDescription("Increase the speed at which the object is transformed by 0.25", null,
                new ConfigurationManagerAttributes { }, true)));

        ConfigEntries.Add(DecreaseTransformSpeed = config.Bind(Hotkeys, "Decrease Transform Speed", new KeyboardShortcut(KeyCode.KeypadMinus),
            new ConfigDescription("Decrease the speed at which the object is transformed by 0.25", null,
                new ConfigurationManagerAttributes { }, true)));

        ConfigEntries.Add(PositionModeKey = config.Bind(Hotkeys, "Position Mode", new KeyboardShortcut(KeyCode.Keypad7), new ConfigDescription(
            "Change to Position mode.", null, new ConfigurationManagerAttributes { }, true)));

        ConfigEntries.Add(ScaleModeKey = config.Bind(Hotkeys, "Scale Mode", new KeyboardShortcut(KeyCode.Keypad8), new ConfigDescription(
            "Change to Scale mode.", null, new ConfigurationManagerAttributes { }, true)));

        ConfigEntries.Add(RotateModeKey = config.Bind(Hotkeys, "Rotate Mode", new KeyboardShortcut(KeyCode.Keypad9), new ConfigDescription(
            "Change to Rotate mode.", null, new ConfigurationManagerAttributes { }, true)));

        #endregion

        #region Config 3. Zone Box Settings

        ConfigEntries.Add(ZoneCubePrefab = config.Bind(ZoneBoxSettings, "Zone Cube Prefab", "", new ConfigDescription(
            "The prefab to use for the Zone Cube (leave empty for cube).", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(TransformSpeed = config.Bind(ZoneBoxSettings, "Transform Speed", 1f, new ConfigDescription(
            "The speed Zone Cube is transformed.", new AcceptableValueRange<float>(0.01f, 30f), new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(SpawnZoneCubeAtLookingPosition = config.Bind(ZoneBoxSettings, "Spawn Zone Cube at Looking Position", false,
            new ConfigDescription(
                "Spawns the Zone Cube at the position you are looking at, if false it will spawn it at your feet.", null,
                new ConfigurationManagerAttributes { }, true)));

        ConfigEntries.Add(LockXAndZRotation = config.Bind(ZoneBoxSettings, "Lock X And Z Rotation Axes", true, new ConfigDescription(
            "Change to Lock X and Z rotation axes.", null, new ConfigurationManagerAttributes { }, true)));

        ConfigEntries.Add(DefaultScale = config.Bind(ZoneBoxSettings, "Default Scale", new Vector3(0.75f, 0.75f, 0.75f), new ConfigDescription(
            "The default scale of the Zone Cube.", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(PositionOffSet = config.Bind(ZoneBoxSettings, "Position OffSet", 0.1f, new ConfigDescription(
            "The offset from the position you are looking at it will spawn.", new AcceptableValueRange<float>(0.0f, 1.0f), new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(ClearZoneCubeDataListOnGenerate = config.Bind(ZoneBoxSettings, "Clear Zone Cube Data List On Generate", true, new ConfigDescription(
            "Clears the Zone Cube Data List when generating the Map Locations file.", null, new ConfigurationManagerAttributes { })));

        #endregion

        #region Config 4. Quest Zone Settings

        ConfigEntries.Add(ZoneId = config.Bind(QuestZoneSettings, "Zone Id", "", new ConfigDescription(
            "The id of the zone (spaces will be replaced with underscores and make it lowercase)", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(ZoneName = config.Bind(QuestZoneSettings, "Zone Name", "", new ConfigDescription(
            "The name of the zone (spaces will be replaced with underscores and make it lowercase)", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(ZoneType = config.Bind(QuestZoneSettings, "Zone Type", EZoneTypes.placeitem, new ConfigDescription(
            "The type of zone", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(FlareType = config.Bind(QuestZoneSettings, "Flare Type", EFlareTypes.none, new ConfigDescription(
            "The type of flare", null, new ConfigurationManagerAttributes { })));

        config.Bind(QuestZoneSettings, "Generate Quest Zone", false, new ConfigDescription(
            "Generates the Zone in 'WTT-ClientCommonLib\\Exports\\Zones'.", null, new ConfigurationManagerAttributes { CustomDrawer = GenerateQuestZoneJson }));

        #endregion

        #region Config 5. Map Locations

        ConfigEntries.Add(config.Bind(MapLocations, "Reset Map Locations", false, new ConfigDescription(
            "Resets the Map Locations List. (not the json file)", null, new ConfigurationManagerAttributes { CustomDrawer = ResetMapLocationsList })));

        ConfigEntries.Add(config.Bind(MapLocations, "Generate Map Locations File", false, new ConfigDescription(
            "Generates the Map Locations List in 'WTT-ClientCommonLib\\Exports'. (appends if exists)", null,
            new ConfigurationManagerAttributes { CustomDrawer = GenerateMapLocationsJson })));

        #endregion

        #region Config 6. Quest Locations

        ConfigEntries.Add(QuestLocationFileName = config.Bind(QuestLocations, "Quest Locations File Name", "QuestLocations", new ConfigDescription(
            "The name of the file that will generate in 'WTT-ClientCommonLib\\Exports'.", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(QuestLocationId = config.Bind(QuestLocations, "Quest Location ID", "", new ConfigDescription(
            "The ID of the quest item location. (mongoid if blank)", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(QuestItemTPL = config.Bind(QuestLocations, "Quest Item TPL", "", new ConfigDescription(
            "The TPL of the quest item.", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(config.Bind(QuestLocations, "Generate Quest Locations", false, new ConfigDescription(
            "Generates the Zone in 'WTT-ClientCommonLib\\Exports'. (appends if exists)", null,
            new ConfigurationManagerAttributes { CustomDrawer = GenerateQuestLocationJson })));

        #endregion

        #region Config 7. Item Spawn Locations

        ConfigEntries.Add(ItemSpawnProbability = config.Bind(ItemSpawnLocations, "Item Spawn Probability", 1.0, new ConfigDescription(
            "The probability of the item spawning at the location (0.0 - 1.0).", new AcceptableValueRange<double>(0.0, 1.0), new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(ItemSpawnIsContainer = config.Bind(ItemSpawnLocations, "Item Spawn Is Container", false, new ConfigDescription(
            "Whether the item spawns as a container.", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(ItemSpawnGravity = config.Bind(ItemSpawnLocations, "Item Spawn Gravity", false, new ConfigDescription(
            "Whether the item spawn has gravity enabled.", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(ItemSpawnRotation = config.Bind(ItemSpawnLocations, "Item Spawn Rotation", false, new ConfigDescription(
            "Whether the item spawns with the rotation given.", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(ItemSpawnIsAlwaysSpawn = config.Bind(ItemSpawnLocations, "Item Spawn Is Always Spawn", false, new ConfigDescription(
            "Whether the item spawn is always spawned.", null, new ConfigurationManagerAttributes { })));

        ConfigEntries.Add(config.Bind(ItemSpawnLocations, "Generate Item Spawn Locations", false, new ConfigDescription(
            "Generates the Item Spawn Locations in 'WTT-ClientCommonLib\\Exports'. (appends if exists)", null,
            new ConfigurationManagerAttributes { CustomDrawer = GenerateItemSpawnLocationJson })));

        #endregion

        #region Subscriptions

        TransformSpeed.Subscribe(value => { });
        ZoneCube.Subscribe(value => { });
        ZonePrefabToggle.Subscribe(value => { });
        CurrentZoneCubePosition.Subscribe(value => { });
        SpawnZoneCubeAtLookingPosition.Subscribe(value => { });
        ZoneCubeTransparency.Subscribe(value => { });
        PositiveXKey.Subscribe(value => { });
        PositiveYKey.Subscribe(value => { });
        PositiveZKey.Subscribe(value => { });
        PositionModeKey.Subscribe(value => { });
        ScaleModeKey.Subscribe(value => { });
        RotateModeKey.Subscribe(value => { });
        LockXAndZRotation.Subscribe(value => { });
        ClearZoneCubeDataListOnGenerate.Subscribe(value => { });

        #endregion

        #region Default Values

        CurrentZoneCubePosition.Value = Vector3.zero;
        CurrentZoneCubeScale.Value = DefaultScale.Value;
        CurrentZoneCubeRotation.Value = Quaternion.identity;
        CurrentMapName.Value = "";
        ZoneId.Value = "";
        ZoneName.Value = "";
        ZoneType.Value = EZoneTypes.placeitem;
        FlareType.Value = EFlareTypes.none;

        #endregion

        RecalcOrder();
    }

    private static void RecalcOrder()
    {
        int settingOrder = ConfigEntries.Count;
        foreach (var entry in ConfigEntries)
        {
            if (entry.Description.Tags[0] is ConfigurationManagerAttributes attributes)
            {
                attributes.Order = settingOrder;
            }

            settingOrder--;
        }
    }

    public static bool IsKeyPressed(KeyboardShortcut key, bool holdKey = false)
    {
        if (holdKey)
        {
            if (!UnityInput.Current.GetKey(key.MainKey))
            {
                return false;
            }

            foreach (var modifier in key.Modifiers)
            {
                if (!UnityInput.Current.GetKey(modifier))
                {
                    return false;
                }
            }
        }
        else
        {
            if (!UnityInput.Current.GetKeyDown(key.MainKey))
            {
                return false;
            }

            foreach (var modifier in key.Modifiers)
            {
                if (!UnityInput.Current.GetKey(modifier))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static bool IsKeyReleased(KeyboardShortcut key)
    {
        if (!UnityInput.Current.GetKey(key.MainKey))
        {
            foreach (var modifier in key.Modifiers)
            {
                if (!UnityInput.Current.GetKey(modifier))
                {
                    return false;
                }
            }

            return true; // Main key and modifiers are no longer pressed
        }

        return false; // Main key is still pressed
    }

    private static void GenerateQuestZoneJson(ConfigEntryBase entry)
    {
        if (GUILayout.Button("Generate Quest Zone", GUILayout.ExpandWidth(true)))
            ExportJsonFile.GenerateJson(ExportJsonFile.JsonType.Zone);
    }

    private static void GenerateMapLocationsJson(ConfigEntryBase entry)
    {
        if (GUILayout.Button("Generate Map Locations File", GUILayout.ExpandWidth(true)))
            ExportJsonFile.GenerateJson(ExportJsonFile.JsonType.MapLocation);
    }

    private static void GenerateQuestLocationJson(ConfigEntryBase entry)
    {
        if (GUILayout.Button("Generate Quest Locations", GUILayout.ExpandWidth(true)))
            ExportJsonFile.GenerateJson(ExportJsonFile.JsonType.QuestLocation);
    }

    private static void GenerateItemSpawnLocationJson(ConfigEntryBase entry)
    {
        if (GUILayout.Button("Generate Item Spawn Locations", GUILayout.ExpandWidth(true)))
            ExportJsonFile.GenerateJson(ExportJsonFile.JsonType.ItemSpawnLocation);
    }

    private static void ResetMapLocationsList(ConfigEntryBase entry)
    {
        if (GUILayout.Button("Reset Map Locations", GUILayout.ExpandWidth(true)))
        {
            if (CurrentMapName.Value == "")
            {
                NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Go to a map to reset Map Locations List.",
                    ENotificationDurationType.Default, ENotificationIconType.Alert);
                return;
            }

            if (CubeDataList.Count == 0)
            {
                WTTClientCommonLib.Logger.LogMessage("[WTT-ClientCommonLib] Map Locations List is already empty.");
                NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Map Locations List is already empty.",
                    ENotificationDurationType.Default, ENotificationIconType.Alert);
                return;
            }

            CubeDataList.Clear();
            WTTClientCommonLib.Logger.LogMessage("[WTT-ClientCommonLib] Map Locations List has been reset.");
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Map Locations List has been reset.");
        }
    }

    private async static void SpawnMapLocationsList(ConfigEntryBase entry)
    {
        if (GUILayout.Button("Spawn Map Location Cubes", GUILayout.ExpandWidth(true)))
        {
            if (CurrentMapName.Value == "")
            {
                NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Go to a map to spawn Map Location Cubes.",
                    ENotificationDurationType.Default, ENotificationIconType.Alert);
                return;
            }

            string filePath = Path.Combine(basePath, "BepInEx", "plugins", "WTT-ClientCommonLib", "Exports", "MapLocations.json");

            MapLocations.MapsLocations mapLocationsData;
            try
            {
                string fileContent = File.ReadAllText(filePath);
                mapLocationsData = JsonConvert.DeserializeObject<MapLocations.MapsLocations>(fileContent);
            }
            catch (Exception ex)
            {
                WTTClientCommonLib.Logger.LogMessage($"[WTT-ClientCommonLib]: Error parsing MapLocations.json: {ex.Message}");
                NotificationManagerClass.DisplayMessageNotification($"[WTT-ClientCommonLib]: Error parsing MapLocations.json: {ex.Message}",
                    ENotificationDurationType.Default, ENotificationIconType.Alert);
                return;
            }

            if (mapLocationsData == null)
            {
                WTTClientCommonLib.Logger.LogMessage("[WTT-ClientCommonLib]: Failed to parse MapLocations.json.");
                NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib]: Failed to parse MapLocations.json.",
                    ENotificationDurationType.Default, ENotificationIconType.Alert);
                return;
            }

            if (!MapIdToNameMap.TryGetValue(CurrentMapName.Value, out string location))
            {
                WTTClientCommonLib.Logger.LogMessage($"[WTT-ClientCommonLib]: Unknown map ID '{location}'.");
                NotificationManagerClass.DisplayMessageNotification($"[WTT-ClientCommonLib]: Unknown map ID '{location}'.", ENotificationDurationType.Default,
                    ENotificationIconType.Alert);
                return;
            }

            List<MapLocations.Location> mapLocations = GetMapLocations(location, mapLocationsData);
            if (mapLocations == null || mapLocations.Count == 0)
            {
                WTTClientCommonLib.Logger.LogMessage($"[WTT-ClientCommonLib]: No locations found for {location}.");
                NotificationManagerClass.DisplayMessageNotification($"[WTT-ClientCommonLib]: No locations found for {location}.",
                    ENotificationDurationType.Default, ENotificationIconType.Alert);
                return;
            }

            WTTClientCommonLib.Logger.LogMessage($"[WTT-ClientCommonLib]: Spawning {mapLocations.Count} Zone Cubes on map '{location}'.");
#if DEBUG
            NotificationManagerClass.DisplayMessageNotification($"[WTT-ClientCommonLib]: Spawning {mapLocations.Count} Zone Cubes on map '{location}'.",
                ENotificationDurationType.Default, ENotificationIconType.Alert);
#endif
            for (int i = 0; i < mapLocations.Count; i++)
            {
                var locationData = mapLocations[i];
                GameObject mapLocationsCube = GameObject.Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube),
                    new Vector3(locationData.Position.X, locationData.Position.Y, locationData.Position.Z),
                    Quaternion.Euler(locationData.Rotation.X, locationData.Rotation.Y, locationData.Rotation.Z)
                );
                mapLocationsCube.transform.localScale = DefaultScale.Value;
                mapLocationsCube.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 0.5f);
                await Task.Delay(2);
            }
        }
    }

    private static List<MapLocations.Location> GetMapLocations(string location, MapLocations.MapsLocations mapLocationsData)
    {
        return location switch
        {
            "Interchange" => mapLocationsData.Interchange,
            "FactoryDay" => mapLocationsData.FactoryDay,
            "FactoryNight" => mapLocationsData.FactoryNight,
            "Customs" => mapLocationsData.Customs,
            "Woods" => mapLocationsData.Woods,
            "Lighthouse" => mapLocationsData.Lighthouse,
            "Shoreline" => mapLocationsData.Shoreline,
            "Reserve" => mapLocationsData.Reserve,
            "Laboratory" => mapLocationsData.Laboratory,
            "StreetsOfTarkov" => mapLocationsData.StreetsOfTarkov,
            "GroundZero" => mapLocationsData.GroundZero,
            "GroundZero21+" => mapLocationsData.GroundZero21,
            "Labyrinth" => mapLocationsData.Labyrinth,
            _ => null
        };
    }

    public static readonly Dictionary<string, string> MapIdToNameMap = new()
    {
        { "Interchange", "Interchange" },
        { "factory4_day", "FactoryDay" },
        { "factory4_night", "FactoryNight" },
        { "bigmap", "Customs" },
        { "Woods", "Woods" },
        { "Lighthouse", "Lighthouse" },
        { "Shoreline", "Shoreline" },
        { "RezervBase", "Reserve" },
        { "laboratory", "Laboratory" },
        { "TarkovStreets", "StreetsOfTarkov" },
        { "Sandbox", "Sandbox" },
        { "Sandbox_high", "Sandbox_high" },
        { "labyrinth", "Labyrinth" }
    };

    public class Location
    {
        public Vector3 Position { get; set; } = Vector3.zero;
        public Vector3 Rotation { get; set; } = Vector3.zero;
    }
}

internal static class SettingExtensions
{
    public static void Subscribe<T>(this ConfigEntry<T> configEntry, Action<T> onChange, bool notification = false)
    {
        configEntry.SettingChanged += (_, _) =>
        {
            onChange(configEntry.Value);
            if (notification)
                NotificationManagerClass.DisplayMessageNotification($"[WTT-ClientCommonLib] Setting {configEntry.Value} changed to {configEntry.Value}");
        };
    }

    public static void Bind<T>(this ConfigEntry<T> configEntry, Action<T> onChange, bool notification = false)
    {
        configEntry.Subscribe(onChange, notification);
        onChange(configEntry.Value);
    }
}