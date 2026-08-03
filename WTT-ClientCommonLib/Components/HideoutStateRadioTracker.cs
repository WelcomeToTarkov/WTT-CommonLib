using UnityEngine;
using WTTClientCommonLib.Configuration;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Components;

public enum RadioLocation
{
    Unknown,
    Gym,
    RestSpace,
}

public class HideoutRadioStateTracker : MonoBehaviour
{
    public RadioLocation Location { get; private set; }
    public bool HasPlayedFirstEntranceAudio { get; set; }
    public float ReplacementChance { get; set; } = 100f;
    public bool Enabled { get; set; } = true;

    private const string GymParentPath = "level1";
    private const string RestSpaceParentPath = "level3";

    void Awake()
    {
        DetermineLocation();
        ApplyConfigSettings();

#if DEBUG
        LogHelper.LogDebug(
            $"Initialized radio tracker for {Location} with settings: "
                + $"Enabled={Enabled}, Chance={ReplacementChance}%, PlayOnFirst={PlayOnFirstEntrance}"
        );
#endif
    }

    public void RefreshSettings()
    {
        Enabled = GetEnabledSetting();
        ReplacementChance = GetReplacementChance();
        PlayOnFirstEntrance = GetPlayOnFirstEntrance();

        float volume = GetVolumeForLocation();
        var sources = GetComponents<AudioSource>();
        foreach (var source in sources)
        {
            source.volume = volume;
        }

#if DEBUG
        LogHelper.LogDebug($"Refreshed settings for {Location} radio");
#endif
    }

    private float GetVolumeForLocation()
    {
        return Location switch
        {
            RadioLocation.Gym => RadioSettings.GymRadioVolume.Value,
            RadioLocation.RestSpace => RadioSettings.RestSpaceRadioVolume.Value,
            _ => 1f,
        };
    }

    private bool GetEnabledSetting() =>
        Location switch
        {
            RadioLocation.Gym => RadioSettings.GymEnabled.Value,
            RadioLocation.RestSpace => RadioSettings.RestSpaceEnabled.Value,
            _ => false,
        };

    private float GetReplacementChance() =>
        Location switch
        {
            RadioLocation.Gym => RadioSettings.GymReplacementChance.Value,
            RadioLocation.RestSpace => RadioSettings.RestSpaceReplacementChance.Value,
            _ => 100f,
        };

    private bool GetPlayOnFirstEntrance() =>
        Location switch
        {
            RadioLocation.Gym => RadioSettings.GymPlayOnFirstEntrance.Value,
            RadioLocation.RestSpace => RadioSettings.RestSpacePlayOnFirstEntrance.Value,
            _ => true,
        };

    public void ApplyConfigSettings()
    {
        switch (Location)
        {
            case RadioLocation.Gym:
                Enabled = RadioSettings.GymEnabled.Value;
                ReplacementChance = RadioSettings.GymReplacementChance.Value;
                PlayOnFirstEntrance = RadioSettings.GymPlayOnFirstEntrance.Value;
                break;

            case RadioLocation.RestSpace:
                Enabled = RadioSettings.RestSpaceEnabled.Value;
                ReplacementChance = RadioSettings.RestSpaceReplacementChance.Value;
                PlayOnFirstEntrance = RadioSettings.RestSpacePlayOnFirstEntrance.Value;
                break;

            default:
                Enabled = false;
                ReplacementChance = 0f;
                PlayOnFirstEntrance = false;
                break;
        }
    }

    public bool PlayOnFirstEntrance { get; private set; } = true;

    private void DetermineLocation()
    {
        Transform parent = transform.parent;
        int depth = 0;

        while (parent != null && depth < 5)
        {
            if (parent.name.Contains(GymParentPath))
            {
                Location = RadioLocation.Gym;
                return;
            }

            if (parent.name.Contains(RestSpaceParentPath))
            {
                Location = RadioLocation.RestSpace;
                return;
            }

            parent = parent.parent;
            depth++;
        }

        Location = RadioLocation.Unknown;
    }
}
