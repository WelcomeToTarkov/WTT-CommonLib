using System;
using UnityEngine;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Components;

public class RevertClipComponent : MonoBehaviour
{
    private AudioSource _audioSource;
    private AudioClip _originalClip;
    private Action _onRevert;

    public void Initialize(AudioSource audioSource, AudioClip originalClip, Action onRevert)
    {
        _audioSource = audioSource;
        _originalClip = originalClip;
        _onRevert = onRevert;
    }

    private void Update()
    {
        if (!_audioSource || !_originalClip)
        {
            Cleanup();
            return;
        }

        if (!_audioSource.isPlaying && _audioSource.clip != _originalClip)
        {
            _audioSource.clip = _originalClip;
#if DEBUG
            LogHelper.LogDebug($"Reverted audio clip to {_originalClip.name}");
#endif
            _onRevert?.Invoke();
            Cleanup();
        }
    }

    private void Cleanup()
    {
        _onRevert = null;
        Destroy(this);
    }
}
