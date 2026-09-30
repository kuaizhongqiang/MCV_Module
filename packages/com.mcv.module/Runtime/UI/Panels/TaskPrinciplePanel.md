# Contract: TaskPrinciplePanel

Role: experimental principle panel (View); plays a principle video. Which video plays is decided by TaskPrincipleController, and the on-screen output is configured in the editor.

Fields:
processingSlider:Slider  playback progress; drag to seek
playBtn:Button  play/pause (required)
videoDir:string  StreamingAssets subfolder ("Video"); full path is StreamingAssets/{videoDir}/{videoName}.mp4
ScaleMin  const 0.5, the collapsed scale of VideoScale
ControlIdleHideDelay  const 3s idle wait before collapsing the controls
player:VideoPlayer  Unity-native player on this GameObject, created by VideoTool.CreateVideoPlayer; every call goes through the VideoTool statics
progressCoroutine / ScaleChangeCoroutine / controlHideCoroutine:Coroutine  running coroutines
isPlaying:bool  playback state
m_Seeking:bool  the slider is being dragged
m_Ready:bool  Awake completed; every public call gates on it
currentVideoName:string  current video name ("" when none)
CurrentVideoName / IsPlaying  public read-only views

Methods:
LoadVideo(videoName, autoPlay=true)  stop current -> set the path -> (editor) check the file -> preload -> set the slider range -> play when autoPlay
PlayVideo() / PauseVideo() / ToggleVideo() / StopVideo()  transport controls; StopVideo resets progress
PauseInternal()  pause body used by the progress routine
SetButtonICO(isPlay)  swap the play/pause icon by child index [0][1] / [0][2]
BuildVideoPath(videoName)  StreamingAssets/{videoDir}/{videoName}.mp4
OnSliderDragStateChanged(dragging)  seek only on release
StartProgressSync() / StopProgressSync()  manage the progress coroutine
OnMouseMoveStateEvent(e)  idle -> delayed collapse; moving -> restore now
IdleHideRoutine() / StopControlHide()  wait ControlIdleHideDelay then collapse / cancel the pending collapse
ProgressSyncRoutine()  write the progress each frame; auto-pause at the end
ScaleChange(Min)  scale VideoScale and slide ControlPart

Nested type SliderDragProbe  slider drag probe bound to the progress slider; reports press and release so the panel does not write progress while dragging
Methods:
OnPointerDown(eventData)  raise OnDragStateChanged(true)
OnPointerUp(eventData)  raise OnDragStateChanged(false)

Notes:
- The panel deliberately has no Update; progress runs in a coroutine so it can be stopped cleanly.
- ScaleChange keeps the existing Z: the old new Vector3(x, 1f) wrote Z = 0 and broke the prefab scale.
- LoadVideo is a no-op before Awake (m_Ready / player null); the panel does not lazy-init on the first play.
- OnDestroy must unsubscribe the mouse-move event, or a destroyed panel keeps receiving callbacks.
- On-screen output (RawImage render texture, VideoPlayer renderMode and targetTexture) is editor-side; do not drive it from code.
- Seek happens on slider release only, otherwise the drag and the per-frame write fight each other.
- There is no player abstraction: the panel holds the engine player and calls VideoTool, which is the framework's only video entry point.
