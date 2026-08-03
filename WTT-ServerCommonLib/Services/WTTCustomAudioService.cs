using SPTarkov.Common.Logger;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using WTTServerCommonLib.Helpers;

namespace WTTServerCommonLib.Services;

[Injectable(InjectionType.Singleton)]
public class WTTCustomAudioService(ModHelper modHelper, SptLogger<WTTCustomAudioService> logger)
{
    private readonly List<string> _audioBundleKeys = new();
    private readonly Dictionary<string, FaceCardAudioEntry> _faceCardAudio = new();
    private readonly List<string> _radioAudio = new();

    /// <summary>
    /// Registers audio bundle keys that are already in your mods SPT bundles.json manifest
    /// </summary>
    /// <param name="audioBundleKeys">List of bundle keys (e.g., "heads/bigbossaudio.bundle")</param>
    public void RegisterAudioBundles(List<string> audioBundleKeys)
    {
        foreach (var bundleKey in audioBundleKeys)
        {
            _audioBundleKeys.Add(bundleKey);
            LogHelper.Debug(logger, $"[AudioService] Registered audio bundle: {bundleKey}");
        }
    }

    /// <summary>
    /// Adds a custom audio key associated with a specific face name.
    /// Optionally marks the audio to play on radio only when the face is selected.
    /// </summary>
    /// <param name="faceName">The unique identifier of the face card.</param>
    /// <param name="audioKey">The name (key) of the audio clip.</param>
    /// <param name="playOnRadioIfFaceIsSelected">
    /// If true, the audio will be included in the radio pool only when the face is actively selected. Defaults to false.
    /// </param>
    public void CreateFaceCardAudio(
        string faceName,
        string audioKey,
        bool playOnRadioIfFaceIsSelected = false
    )
    {
        if (!_faceCardAudio.TryGetValue(faceName, out var entry))
        {
            entry = new FaceCardAudioEntry();
            _faceCardAudio[faceName] = entry;
        }

        entry.Audio.Add(audioKey);
        entry.PlayOnRadio = playOnRadioIfFaceIsSelected;
        LogHelper.Debug(logger, $"[AudioService] Added face audio: {faceName} → {audioKey}");
    }

    /// <summary>
    /// Adds an audio key to the global radio audio pool, which plays independent of face selection.
    /// </summary>
    /// <param name="audioKey">The name of the radio audio clip.</param>
    public void CreateRadioAudio(string audioKey)
    {
        _radioAudio.Add(audioKey);
        LogHelper.Debug(logger, $"[AudioService] Added radio audio: {audioKey}");
    }

    public AudioManifest GetAudioManifest()
    {
        return new AudioManifest
        {
            AudioBundles = _audioBundleKeys,
            FaceCardMappings = _faceCardAudio,
            RadioAudio = _radioAudio,
        };
    }
}

public class AudioManifest
{
    public List<string> AudioBundles { get; set; } = new();
    public Dictionary<string, FaceCardAudioEntry> FaceCardMappings { get; set; } = new();
    public List<string> RadioAudio { get; set; } = new();
}

public class FaceCardAudioEntry
{
    public List<string> Audio { get; set; } = new();
    public bool PlayOnRadio { get; set; } = false;
}
