using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class SoundEffect : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    private void OnDisable()
    {
        audioSource.Stop();
    }

    /// <summary>
    /// Set the sound effect to play    设置音效播放
    /// </summary>
    /// <param name="soundEffect"></param>
    public void SetSound(SoundEffectSO soundEffect)
    {
        audioSource.pitch = Random.Range(soundEffect.soundEffectPitchRandomVariationMin,
            soundEffect.soundEffectPitchRandomVariationMax);
        audioSource.volume = soundEffect.soundEffectVolume;
        audioSource.clip = soundEffect.soundEffectClip;
    }
    
    
}
