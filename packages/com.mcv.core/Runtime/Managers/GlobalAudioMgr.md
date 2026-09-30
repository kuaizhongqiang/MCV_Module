# Contract: GlobalAudioMgr

Role: audio hub (BGM/voice/effect) + volume fade; callers reach it through EventBus, never directly.

Fields:
audioDic:Dictionary<AudioSouceType, AudioStruct>  one AudioSource per source type, created under this transform
audioEffectDic:Dictionary<AudioEffectType, AudioClip>  effects preloaded from Resources/Audio/<enum name>
audioNameCache:Dictionary<string, AudioClip>  on-demand clips (BGM/voice), avoids repeated Resources.Load
volumeDuration:float  fade length in seconds, 1.5
volumeCoroutines:Dictionary<AudioSouceType, Coroutine>  running fade per source type

Methods:
DelayInit()  create all sources -> preload effects (AudioEffectType.None skipped) -> subscribe 3 EventBus events
OnDestroy()  unsubscribe the same 3
PlayAudio(audioName, type=Speaker)  static; publish AudioPlayEventData
PlayAudio(type)  static; publish AudioPlayEffectEventData
SetVolume(type, volume)  static; publish AudioVolumeEventData (fades)
SetVolumeImmediate(type, volume)  static; set now and cancel the running fade
OnVolumeChangeRequest / OnPlayEffectRequest / OnPlayAudioRequest  EventBus callbacks
StartVolumeTransition(type, target)  cancel a running fade and start a new one; no-op when already at target
SetVolumeAnim(type, target)  Lerp over volumeDuration
PlayEffectInternal(type)  PlayOneShot on the Effect source
PlayAudioInternal(name, type)  load with cache, then set clip and Play
CreateAudioStruct(type)  child GameObject named after the type with a configured AudioSource
AudioStruct  [struct] audioSource/audioClip/volume

Notes:
- SetVolumeImmediate bails out while isInit is false: the sources do not exist yet, so early calls are dropped by design.
- Effect clips are looked up by enum name; a missing clip only logs a warning.
- Sources are created as children of this manager's transform.
