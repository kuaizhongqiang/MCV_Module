# Contract: VideoComponent

Role: video display data holder for the built-in Unity (Legacy RawImage) playback host.

Fields:
videoType:VideoType  [SerializeField] only Legacy exists (no third-party player kind)
videoPath:string  [SerializeField] resource path
legacyVideoPlayer:RawImage  [SerializeField] the Legacy render target host

Notes:
- Only the Unity-native host exists here: there is no plugin field, nothing to assign, and no player abstraction to implement. A host that needs another player drives it entirely on its own side.
