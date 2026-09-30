# Contract: RoomOneDynamicMgr

Role: scene-level dynamics of room 1: a two-layer HUD background video (intro then loop) plus the project HUD icons taken from the RoomOne global package.

Fields:
onceAlphaVideoPlayer / onceColorVideoPlayer / loopAlphaVideoPlayer / loopColorVideoPlayer:VideoPlayer  [SerializeField] the four players
onceVideoDuration:float  [SerializeField] intro fallback length in seconds, 1
menuObjsRoot:Transform  [SerializeField] parent of the RoomMenuObj HUDs; empty = skip icon loading with a warning
onceAlphaVideoPath / onceColorVideoPath / loopAlphaVideoPath / loopColorVideoPath  const StreamingAssets-relative mp4 paths
PrepareTimeout:float  const 5; per-player wait for Prepare
m_OnceFinished:bool  set from loopPointReached
m_Destroyed:bool  guards the async icon callback
m_LoadedIconIds:List<string>  package ids requested this time

Methods:
DelayInit()  one frame -> LoadMenuIcons() (not awaited) -> Validate() (isInit either way) -> Configure -> PlayRoutine
OnDestroy()  m_Destroyed = true -> unsubscribe loopPointReached -> UnloadByBundleName(RoomOneBundleName) when icons were loaded -> clear the singleton when it is this
LoadMenuIcons()  per RoomMenuObj child: ContentNaming.RoomIconId(ProjectName) -> LoadSpritesByPackageIdsAsync -> SetIcon on the live HUDs
Validate()  all four VideoPlayers present, else Log.Error and the video part is skipped
Configure()  url / isLooping / playOnAwake=false + Prepare per player, then subscribe loopPointReached once
Setup(player, url, isLooping)  static helper
OnOnceLoopPointReached(player)  m_OnceFinished = true
PlayRoutine()  wait both intro players prepared -> play as a pair -> wait for m_OnceFinished or the length fallback -> stop the pair -> wait both loop players -> play as a pair
GetOnceLimit()  player.length when readable, else onceVideoDuration, +0.5s slack
WaitPrepared(player)  poll isPrepared under PrepareTimeout
PlayPair(alpha, color) / StopPair(alpha, color)  static; both layers act on the same frame

Notes:
- Alpha and color are two RenderTextures: onceAlpha/loopAlpha -> ModelShow_Alpha_Small, onceColor/loopColor -> ModelShow_Color_Small. The room HUD RawImage (ChildMenuCanvas) reads that pair, so the two players feeding one RT must never overlap -- hence intro-then-loop and StopPair before PlayPair.
- m_Url is empty in the prefab and playOnAwake is off; the paths are assembled here from StreamingAssets, so swapping a video is a constant edit and art cannot misconfigure isLooping.
- The RoomOne package is global (clipId empty) and deliberately outside GlobalAssetsMgr's clip install/uninstall chain, so this manager drives its load and unload; its lifetime equals the room scene's.
- Icon ids are derived from each HUD's own ProjectName, not matched against the request list, because LoadSpritesByPackageIdsAsync skips failures and may return fewer items than asked for.
- Awake is not overridden: the base handles the singleton and starts DelayInit only for the winning instance, so two instances can never fight over the same RT pair.
- isInit is set even when Validate fails, otherwise IsInit would stay false and outside waiters would hang.
- The intro wait is bounded on purpose: a decode failure never raises loopPointReached.
