using System;
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Singleton;
using UnityEngine;

namespace MCV_Module.Managers
{
    public class GlobalAudioMgr : SingletonGlobalMgr<GlobalAudioMgr>
    {
        #region 参数
        Dictionary<AudioSouceType, AudioStruct> audioDic = new Dictionary<AudioSouceType, AudioStruct>();
        Dictionary<AudioEffectType, AudioClip> audioEffectDic = new Dictionary<AudioEffectType, AudioClip>();
        // 按名称缓存按需加载的音频（BGM/语音），避免每次播放都 Resources.Load
        readonly Dictionary<string, AudioClip> audioNameCache = new Dictionary<string, AudioClip>();

        float volumeDuration = 1.5f;

        Dictionary<AudioSouceType, Coroutine> volumeCoroutines = new Dictionary<AudioSouceType, Coroutine>();
        #endregion

        #region 生命周期
        protected override IEnumerator DelayInit()
        {
            foreach (AudioSouceType audioSouceType in Enum.GetValues(typeof(AudioSouceType)))
            {
                audioDic.Add(audioSouceType, CreateAudioStruct(audioSouceType));
            }

            foreach (AudioEffectType audioEffectType in Enum.GetValues(typeof(AudioEffectType)))
            {
                if (audioEffectType == AudioEffectType.None) continue; // None = 不播放，跳过加载
                AudioClip audioClip = Resources.Load<AudioClip>("Audio/" + audioEffectType.ToString());
                if (audioClip != null)
                {
                    audioEffectDic.Add(audioEffectType, audioClip);
                }
                else
                {
                    Debug.LogWarning("没有找到音频：" + audioEffectType.ToString());
                }
            }

            // 注册 EventBus 事件监听
            EventBus<AudioVolumeEventData>.Subscribe(OnVolumeChangeRequest);
            EventBus<AudioPlayEffectEventData>.Subscribe(OnPlayEffectRequest);
            EventBus<AudioPlayEventData>.Subscribe(OnPlayAudioRequest);

            yield return null;
            isInit = true;
        }

        protected override void OnDestroy()
        {
            EventBus<AudioVolumeEventData>.Unsubscribe(OnVolumeChangeRequest);
            EventBus<AudioPlayEffectEventData>.Unsubscribe(OnPlayEffectRequest);
            EventBus<AudioPlayEventData>.Unsubscribe(OnPlayAudioRequest);
            base.OnDestroy();
        }
        #endregion

        #region 静态方法
        #region 播放音频
        public static void PlayAudio(string audioName,
            AudioSouceType type = AudioSouceType.Speaker)
        {
            EventBus<AudioPlayEventData>.Publish(
                new AudioPlayEventData(audioName, type));
        }

        public static void PlayAudio(AudioEffectType type)
        {
            EventBus<AudioPlayEffectEventData>.Publish(
                new AudioPlayEffectEventData(type));
        }
        #endregion

        #region 音量控制
        public static void SetVolume(AudioSouceType audioSouceType, float volume)
        {
            EventBus<AudioVolumeEventData>.Publish(
                new AudioVolumeEventData(audioSouceType, volume));
        }

        public static void SetVolumeImmediate(AudioSouceType audioSouceType, float volume)
        {
            if (!Instance.isInit) return;

            if (Instance.audioDic.TryGetValue(audioSouceType, out AudioStruct audioStruct))
            {
                audioStruct.audioSource.volume = volume;
                if (Instance.volumeCoroutines.TryGetValue(audioSouceType, out Coroutine coroutine))
                {
                    if (coroutine != null)
                        Instance.StopCoroutine(coroutine);
                    Instance.volumeCoroutines[audioSouceType] = null;
                }
            }
        }
        #endregion
        #endregion

        #region 私有方法
        // ── EventBus 事件回调 ──────────────────────────────────

        void OnVolumeChangeRequest(AudioVolumeEventData data)
        {
            StartVolumeTransition(data.SourceType, data.TargetVolume);
        }

        void OnPlayEffectRequest(AudioPlayEffectEventData data)
        {
            PlayEffectInternal(data.EffectType);
        }

        void OnPlayAudioRequest(AudioPlayEventData data)
        {
            PlayAudioInternal(data.AudioName, data.SourceType);
        }

        // ── 内部实现 ───────────────────────────────────────────

        void StartVolumeTransition(AudioSouceType audioSouceType, float targetVolume)
        {
            if (!audioDic.TryGetValue(audioSouceType, out AudioStruct audioStruct)) return;

            AudioSource audio = audioStruct.audioSource;

            if (Mathf.Approximately(audio.volume, targetVolume)) return;

            if (volumeCoroutines.TryGetValue(audioSouceType, out Coroutine existingCoroutine))
            {
                if (existingCoroutine != null)
                    StopCoroutine(existingCoroutine);
            }

            Coroutine newCoroutine = StartCoroutine(SetVolumeAnim(audioSouceType, targetVolume));
            volumeCoroutines[audioSouceType] = newCoroutine;
        }

        IEnumerator SetVolumeAnim(AudioSouceType audioSouceType, float targetVolume)
        {
            AudioSource audio = audioDic[audioSouceType].audioSource;
            float currentVolume = audio.volume;
            float time = 0;

            while (time < volumeDuration)
            {
                time += Time.deltaTime;
                audio.volume = Mathf.Lerp(currentVolume, targetVolume, time / volumeDuration);
                yield return null;
            }

            audio.volume = targetVolume;
            volumeCoroutines[audioSouceType] = null;
        }

        void PlayEffectInternal(AudioEffectType type)
        {
            if (audioEffectDic.TryGetValue(type, out AudioClip audioClip))
            {
                audioDic[AudioSouceType.Effect].audioSource.PlayOneShot(audioClip);
            }
        }

        void PlayAudioInternal(string audioName, AudioSouceType type)
        {
            if (audioDic.TryGetValue(type, out AudioStruct audioStruct))
            {
                if (!audioNameCache.TryGetValue(audioName, out AudioClip clip))
                {
                    clip = Resources.Load<AudioClip>("Audio/" + audioName);
                    if (clip != null) audioNameCache[audioName] = clip;
                }
                if (clip != null)
                {
                    audioStruct.audioSource.clip = clip;
                    audioStruct.audioSource.Play();
                }
            }
        }
        #endregion

        #region 工具方法
        AudioStruct CreateAudioStruct(AudioSouceType audioSouceType)
        {
            AudioStruct audioStruct = new AudioStruct();
            audioStruct.audioSource = new GameObject(audioSouceType.ToString()).AddComponent<AudioSource>();
            audioStruct.audioSource.transform.SetParent(transform);
            audioStruct.audioSource.playOnAwake = false;
            audioStruct.audioSource.loop = false;
            audioStruct.audioSource.volume = 1;

            return audioStruct;
        }
        #endregion

        #region 其他类
        struct AudioStruct
        {
            public AudioSource audioSource;
            public AudioClip audioClip;
            public float volume;
        }
        #endregion
    }
}
