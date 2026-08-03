using System;
using BepInEx.Configuration;
using WTTClientCommonLib.Attributes;
using WTTClientCommonLib.Components;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Configuration;

internal static class RadioSettings
{
    public static ConfigEntry<float> FaceCardVolume;

    public static ConfigEntry<bool> GymEnabled;
    public static ConfigEntry<float> GymReplacementChance;
    public static ConfigEntry<bool> GymPlayOnFirstEntrance;
    public static ConfigEntry<float> GymRadioVolume;

    public static ConfigEntry<bool> RestSpaceEnabled;
    public static ConfigEntry<float> RestSpaceReplacementChance;
    public static ConfigEntry<bool> RestSpacePlayOnFirstEntrance;
    public static ConfigEntry<float> RestSpaceRadioVolume;

    public static void Init(ConfigFile config)
    {
        FaceCardVolume = config.Bind(
            "FaceCard",
            "Music Volume",
            .06f,
            new ConfigDescription(
                "Volume for the FaceCard custom audio",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 9 }
            )
        );

        GymEnabled = config.Bind(
            "Gym Radio",
            "Enabled",
            true,
            new ConfigDescription(
                "Enable/disable custom audio for the Gym radio",
                null,
                new ConfigurationManagerAttributes { Order = 8 }
            )
        );

        GymRadioVolume = config.Bind(
            "Gym Radio",
            "Gym Radio Volume",
            0.6f,
            new ConfigDescription(
                "Volume for the Gym Radio custom audio)",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 7 }
            )
        );

        GymReplacementChance = config.Bind(
            "Gym Radio",
            "Replacement Chance",
            10f,
            new ConfigDescription(
                "Percentage chance to replace gym radio audio (0-100)",
                new AcceptableValueRange<float>(0f, 100f),
                new ConfigurationManagerAttributes { Order = 6 }
            )
        );

        GymPlayOnFirstEntrance = config.Bind(
            "Gym Radio",
            "Play On First Entrance",
            true,
            new ConfigDescription(
                "Play custom audio when first entering hideout",
                null,
                new ConfigurationManagerAttributes { Order = 5 }
            )
        );

        RestSpaceEnabled = config.Bind(
            "Rest Space Radio",
            "Enabled",
            true,
            new ConfigDescription(
                "Enable/disable custom audio for the Rest Space radio",
                null,
                new ConfigurationManagerAttributes { Order = 4 }
            )
        );

        RestSpaceRadioVolume = config.Bind(
            "Rest Space Radio",
            "Rest Space Radio Volume",
            0.6f,
            new ConfigDescription(
                "Volume for the Rest Space Radio custom audio)",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 3 }
            )
        );

        RestSpaceReplacementChance = config.Bind(
            "Rest Space Radio",
            "Replacement Chance",
            10f,
            new ConfigDescription(
                "Percentage chance to replace rest space radio audio (0-100)",
                new AcceptableValueRange<float>(0f, 100f),
                new ConfigurationManagerAttributes { Order = 2 }
            )
        );

        RestSpacePlayOnFirstEntrance = config.Bind(
            "Rest Space Radio",
            "Play On First Entrance",
            true,
            new ConfigDescription(
                "Play custom audio when first entering hideout",
                null,
                new ConfigurationManagerAttributes { Order = 1 }
            )
        );

        FaceCardVolume.SettingChanged += ConfigSettingChanged;
        GymEnabled.SettingChanged += ConfigSettingChanged;
        GymRadioVolume.SettingChanged += ConfigSettingChanged;
        GymReplacementChance.SettingChanged += ConfigSettingChanged;
        GymPlayOnFirstEntrance.SettingChanged += ConfigSettingChanged;
        RestSpaceEnabled.SettingChanged += ConfigSettingChanged;
        RestSpaceRadioVolume.SettingChanged += ConfigSettingChanged;
        RestSpaceReplacementChance.SettingChanged += ConfigSettingChanged;
        RestSpacePlayOnFirstEntrance.SettingChanged += ConfigSettingChanged;
    }

    private static void ConfigSettingChanged(object sender, EventArgs e)
    {
        RefreshAllTrackers();
#if DEBUG
        LogHelper.LogDebug("Config changed - refreshing radio settings");
#endif
    }

    private static void RefreshAllTrackers()
    {
        var trackers = UnityEngine.Object.FindObjectsOfType<HideoutRadioStateTracker>();
        foreach (var tracker in trackers)
        {
            tracker.RefreshSettings();
#if DEBUG
            LogHelper.LogDebug($"Refreshed settings for {tracker.Location} radio");
#endif
        }
    }
}
