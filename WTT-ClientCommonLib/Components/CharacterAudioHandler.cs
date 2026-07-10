using System;
using System.Collections;
using UnityEngine;
using WTTClientCommonLib.Configuration;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Components;

public class CharacterAudioHandler : MonoBehaviour
{
    private AudioSource _audioSource;
    private bool _isInitialized;
    private bool _isFadingIn;
    private bool _isFadingOut;
    private Coroutine _fadeCoroutine;

    public void Initialize(string faceName)
    {
        LogHelper.LogDebug($"CharacterAudioHandler.Initialize called with faceName={faceName}");

        var audioKey = ResourceLoader.GetAudioForFace(faceName);

        if (string.IsNullOrEmpty(audioKey))
        {
            LogHelper.LogDebug($"AudioKey is empty for face {faceName}");
            return;
        }

        if (!ResourceLoader.ClipCache.TryGetAudioClip(audioKey, out var audioClip))
        {
            LogHelper.LogDebug($"Audio clip not in cache: {audioKey}");
            return;
        }

        LogHelper.LogDebug($"Found audio clip in cache: {audioKey}");
        LogHelper.LogDebug(
            $"Clip details - duration: {audioClip.length}s, samples: {audioClip.samples}, channels: {audioClip.channels}"
        );

        AssignAudioToSource(audioClip, faceName);
        RadioSettings.FaceCardVolume.SettingChanged += OnFaceCardVolumeChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        RadioSettings.FaceCardVolume.SettingChanged -= OnFaceCardVolumeChanged;
    }

    private void OnFaceCardVolumeChanged(object sender, EventArgs e)
    {
        if (!_isInitialized || _audioSource == null)
            return;

        if (_audioSource.isPlaying && !_isFadingOut)
        {
            _audioSource.volume = RadioSettings.FaceCardVolume.Value;
            LogHelper.LogDebug($"Updated audio volume: {_audioSource.volume}");
        }
    }

    public void AssignAudioToSource(AudioClip audioClip, string originalName)
    {
        LogHelper.LogDebug($"AssignAudioToSource: clip={audioClip?.name}, name={originalName}");

        _audioSource = gameObject.GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            LogHelper.LogDebug($"Created new AudioSource");
        }
        else
        {
            LogHelper.LogDebug($"Reusing existing AudioSource");
        }

        _audioSource.clip = audioClip;
        _audioSource.loop = true;
        _audioSource.volume = 0f;
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
        _isInitialized = true;

        LogHelper.LogDebug(
            $"Audio source configured: spatialBlend={_audioSource.spatialBlend}, volume={_audioSource.volume}"
        );
    }

    public void FadeIn()
    {
        if (!_isInitialized)
        {
            LogHelper.LogWarn("FadeIn called but handler not initialized");
            return;
        }

        LogHelper.LogDebug($"FadeIn: Starting fade-in, current volume: {_audioSource?.volume}");

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            LogHelper.LogDebug("Stopped previous fade coroutine");
        }

        _fadeCoroutine = StartCoroutine(FadeAudio(RadioSettings.FaceCardVolume.Value, 5f));
    }

    public void FadeOut()
    {
        if (!_isInitialized)
        {
            LogHelper.LogWarn("FadeOut called but handler not initialized");
            return;
        }

        LogHelper.LogDebug($"FadeOut: Starting fade-out, current volume: {_audioSource?.volume}");

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            LogHelper.LogDebug("Stopped previous fade coroutine");
        }

        _fadeCoroutine = StartCoroutine(FadeAudio(0f, 5f));
    }

    private IEnumerator FadeAudio(float targetVolume, float duration)
    {
        if (_audioSource == null || _audioSource.clip == null)
        {
            LogHelper.LogWarn("FadeAudio: AudioSource or clip is null");
            yield break;
        }

        _isFadingIn = targetVolume > 0;
        _isFadingOut = targetVolume == 0;

        LogHelper.LogDebug(
            $"FadeAudio started: targetVolume={targetVolume}, duration={duration}, isFadingIn={_isFadingIn}"
        );

        float startVolume = _audioSource.volume;
        float time = 0f;

        if (_isFadingIn && !_audioSource.isPlaying)
        {
            _audioSource.Play();
            LogHelper.LogDebug("Audio started playing");
        }

        while (time < duration && _audioSource != null)
        {
            time += Time.deltaTime;

            float currentTarget = _isFadingIn ? RadioSettings.FaceCardVolume.Value : targetVolume;

            _audioSource.volume = Mathf.Lerp(startVolume, currentTarget, time / duration);
            yield return null;
        }

        if (_audioSource != null)
        {
            _audioSource.volume = _isFadingIn ? RadioSettings.FaceCardVolume.Value : targetVolume;

            LogHelper.LogDebug(
                $"FadeAudio completed: final volume={_audioSource.volume}, isFadingOut={_isFadingOut}"
            );

            if (_isFadingOut)
            {
                _audioSource.Stop();
                LogHelper.LogDebug("Audio stopped");
            }
        }

        _isFadingIn = false;
        _isFadingOut = false;
    }
}
