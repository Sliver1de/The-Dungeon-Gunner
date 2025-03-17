using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class SoundEffectManager : SingletonMonobehaviour<SoundEffectManager>
{
    public int soundsVolume = 8;

    private void Start()
    {
        if (PlayerPrefs.HasKey("soundsVolume"))
        {
            soundsVolume = PlayerPrefs.GetInt("soundsVolume");
        }

        SetsoundsVolume(soundsVolume);
    }

    private void OnDisable()
    {
        //Save volume settings in playerprefs   将音效设置保存在 PlayerPrefs 中
        PlayerPrefs.SetInt("soundsVolume", soundsVolume);
    }

    /// <summary>
    /// Play the sound effect   播放音效
    /// </summary>
    /// <param name="soundEffect"></param>
    public void PlaySoundEffect(SoundEffectSO soundEffect)
    {
        //Play sound using a sound gameobject and component from the object pool    使用对象池中的声音游戏对象和组件播放音效
        SoundEffect sound =
            (SoundEffect)PoolManager.Instance.ReuseComponent(soundEffect.soundPrefab, Vector3.zero,
                quaternion.identity);
        sound.SetSound(soundEffect);
        sound.gameObject.SetActive(true);
        StartCoroutine(DisableSound(sound, soundEffect.soundEffectClip.length));
    }

    /// <summary>
    /// Disable sound effect object after it has player thus returning it to the object pool
    /// 在音效播放完毕后禁用其对象，从而将其返回到对象池
    /// </summary>
    /// <param name="sound"></param>
    /// <param name="soundDuration"></param>
    /// <returns></returns>
    private IEnumerator DisableSound(SoundEffect sound, float soundDuration)
    {
        yield return new WaitForSeconds(soundDuration);
        sound.gameObject.SetActive(false);
    }

    /// <summary>
    /// Increase music volume
    /// </summary>
    public void IncreaSoundsVolume()
    {
        int maxsoundsVolume = 20;

        if (soundsVolume >= maxsoundsVolume) return;

        soundsVolume += 1;
        
        SetsoundsVolume(soundsVolume);
    }

    /// <summary>
    /// Decrease music volume
    /// </summary>
    public void DecreaseSoundsVolume()
    {
        if (soundsVolume == 0) return;
        
        soundsVolume -= 1;
        
        SetsoundsVolume(soundsVolume);
    }
    
    /// <summary>
    /// Set sounds volume   设置音效音量
    /// </summary>
    /// <param name="soundsVolume"></param>
    private void SetsoundsVolume(int soundsVolume)
    {
        float muteDecibels = -80f;

        if (muteDecibels == 0)
        {
            GameResources.Instance.soundsMasterMixerGroup.audioMixer.SetFloat("soundsVolume", muteDecibels);
        }
        else
        {
            GameResources.Instance.soundsMasterMixerGroup.audioMixer.SetFloat("soundsVolume",
                HelperUtilities.LinearToDecibels(soundsVolume));
        }
    }
}
