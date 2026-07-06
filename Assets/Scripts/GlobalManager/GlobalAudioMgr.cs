
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MCV_Module.GlobalManager
{
    public class GlobalAudioMgr : SingletonGlobalMgr<GlobalAudioMgr>
    {
        protected GlobalAudioMgr(){}
        Dictionary<Audio_Type, AudioStruct> audioStructs = new Dictionary<Audio_Type, AudioStruct>();
        [Header("声音混合时间"),SerializeField] float fadeDuration = 1f;
        #region 静态事件
        [HideInInspector] public static UnityEvent<Audio_Type,string> AudioPlayEvent = new UnityEvent<Audio_Type,string>(); 
        [HideInInspector] public static UnityEvent<UIAudioType> UIAudioPlayEvent = new UnityEvent<UIAudioType>();
        [HideInInspector] public static UnityEvent<Audio_Type,float> AudioVolumeEvent = new UnityEvent<Audio_Type,float>();
        #endregion

        protected override void Awake()
        {
            base.Awake();

            AudioPlayEvent.AddListener(PlayAudio);
            UIAudioPlayEvent.AddListener(PlayUIAudio);
            AudioVolumeEvent.AddListener(SetVolume);
        }

        protected override IEnumerator DelayInit()
        {
            audioStructs.Add(Audio_Type.BGM, AudioSourceInit(Audio_Type.BGM));
            audioStructs.Add(Audio_Type.Speaker, AudioSourceInit(Audio_Type.Speaker));
            audioStructs.Add(Audio_Type.UI, AudioSourceInit(Audio_Type.UI));

            isInit = true;
            yield break;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            AudioPlayEvent.RemoveListener(PlayAudio);
            UIAudioPlayEvent.RemoveListener(PlayUIAudio);
            AudioVolumeEvent.RemoveListener(SetVolume);
        }

        AudioStruct AudioSourceInit(Audio_Type type)
        {
            AudioStruct audioStruct = new AudioStruct();

            audioStruct.audioSource = new GameObject("AudioSource").AddComponent<AudioSource>();
            audioStruct.audioSource.playOnAwake = false;
            audioStruct.audioSource.loop = false;
            audioStruct.audioSource.volume = 1;
            audioStruct.audioSource.clip = null;
            audioStruct.audioVolumeCoroutine = null;
            audioStruct.audioClip = null;
            audioStruct.isLoop = false;
            audioStruct.audioSource.gameObject.name = type.ToString();
            audioStruct.audioSource.transform.SetParent(transform);

            return audioStruct;
        }

        #region 工具方法
        AudioSource GetAudioSource(Audio_Type type)
        {
            if (audioStructs.TryGetValue(type, out AudioStruct audioStruct))
            {
                return audioStruct.audioSource;
            }
            return null;
        }

        Coroutine GetCoroutine(Audio_Type type)
        {
            if (audioStructs.TryGetValue(type, out AudioStruct audioStruct))
            {
                return audioStruct.audioVolumeCoroutine;
            }
            return null;
        }

        AudioClip GetAudioClip(Audio_Type type)
        {
            if (audioStructs.TryGetValue(type, out AudioStruct audioStruct))
            {
                return audioStruct.audioClip;
            }
            return null;
        }

        float GetAudioVolume(Audio_Type type)
        {
            if (audioStructs.TryGetValue(type, out AudioStruct audioStruct))
            {
                return audioStruct.volume;
            }
            return 0;
        }
        
        bool GetAudioPlaying(Audio_Type type)
        {
            if (audioStructs.TryGetValue(type, out AudioStruct audioStruct))
            {
                return audioStruct.isPlaying;
            }
            return false;
        }
        
        void SetAudioVolume(Audio_Type type, float volume)
        {
            if (audioStructs.TryGetValue(type, out AudioStruct audioStruct))
            {
                audioStruct.volume = volume;
            }
        }
        
        void SetAudioClip(Audio_Type type, AudioClip audioClip)
        {
            if (audioStructs.TryGetValue(type, out AudioStruct audioStruct))
            {
                audioStruct.audioClip = audioClip;
            }
        }
        #endregion
    
        #region 播放方法
        static void PlayAudioDetail(Audio_Type type, 
            AudioClip audioClip, float volume = 1, bool isLoop = false)
        {
            
        }

        static void PlayAudio(Audio_Type type, string audioClip)
        {
            
        }

        static void PlayUIAudio(UIAudioType type)
        {
            
        }

        static void SetVolume(Audio_Type type, float volume)
        {
            if (Instance.audioStructs.TryGetValue(type, out AudioStruct audioStruct))
            {
                if (audioStruct.audioVolumeCoroutine != null)
                {
                    Instance.StopCoroutine(audioStruct.audioVolumeCoroutine);
                }
                audioStruct.audioVolumeCoroutine = Instance.StartCoroutine(Instance.SetVolumeDelay(type, volume));
            }
        }
        #endregion  

        #region 音量调整
        IEnumerator SetVolumeDelay(Audio_Type type, float volume)
        {
            float currentVolume = GetAudioVolume(type);
            float time = 0;
            float step = (volume - currentVolume) / fadeDuration;

            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                SetAudioVolume(type, currentVolume + step * Time.deltaTime);
                yield return null;
            }

            SetAudioVolume(type, volume);

            yield break;
            
        }
        
        #endregion
    }

    struct AudioStruct
    {
        public AudioSource audioSource;
        public Coroutine audioVolumeCoroutine;
        public AudioClip audioClip;
        public float volume;
        public bool isLoop;
        public bool isPlaying;
    }
    [Serializable]
    public enum Audio_Type
    {
        BGM,
        Speaker,
        UI,
    }
    
    [Serializable]
    public enum UIAudioType
    {
        Click,
        Close,
        Open,
        Select,
        Toggle,
    }
}