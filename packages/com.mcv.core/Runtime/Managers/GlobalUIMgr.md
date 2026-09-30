# Contract: GlobalUIMgr

Role: canvas registry plus state-driven rebuild; canvases register in Awake and unregister in OnDestroy, and every scene-state change fades out the current canvas, clears panels and rebuilds the target.

Fields:
playMode:PlayMode  [SerializeField] Debug enables the on-screen log overlay
canvasDict:Dictionary<string, CanvasBase>  type name -> canvas
m_CurrentState:SceneState  the single navigation state, written only from SceneStateChangeEventData
m_ActiveCanvas:CanvasBase  the canvas currently shown
m_SwitchCoroutine:Coroutine  running switch coroutine, cancelled by the next switch
m_InitialState:SceneState  [SerializeField] initial state published once UI is ready, Start
m_InitialTaskType:TaskType  [SerializeField] Inspector compatibility only; the real source is ProjectData.currentTaskType
m_InitialStatePublished:bool  one-shot guard
ifCopyright / ifCompany:bool  [SerializeField] copyright and company info switches
CanvasRebuildVersion:int  static; +1 after every target-canvas Rebuild
IsSwitching:bool  static; true while a switch coroutine runs
IfCopyright / IfCompany  static property wrappers

Methods:
DelayInit()  toggle the log overlay by playMode -> subscribe SceneStateChangeEventData + TaskTypeChangeEventData + LoginSuccessEvent -> one frame -> DialogEventDispatcher.Initialize -> isInit
OnDestroy()  DialogEventDispatcher.Shutdown plus unsubscribe the three
RegisterCanvas(canvas) / UnregisterCanvas(canvas)  static; dictionary keyed by type name; Unregister returns early when the singleton is gone; Register also tries to publish the initial state
GetCanvas<T>()  static; canvas by type name
GetActiveCanvas()  static; the canvas being shown
GetPanelOnActiveCanvas<T>() / GetPanel<T>()  static; GetPanel scans only enabled, non-persistent canvases
OnSceneStateChanged(e)  -> SwitchToState
OnLoginSuccess(e)  publish SceneState.Menu
OnTaskTypeChanged(e)  SetCurrentTaskType, then rebuild only while the state is UI or Roaming
SwitchToState(state)  pick the non-persistent canvas whose MatchesState is true -> SetProjectState(ToProjectState(state)) -> cancel the previous coroutine -> start SwitchToStateCoroutine
ToProjectState(state)  static; SceneState -> ProjectState (Setup/Start/Login -> Start)
SwitchToStateCoroutine(target, state, all)  fade out prev -> hide and ClearPanels on every non-target canvas -> activate, Rebuild, CanvasRebuildVersion++, fade in
CurrentStateDescription()  AI context text: current scene, plus task type for UI/Roaming
SceneStateDescription(state)  static; Chinese description per state
TryPublishInitialState() / PublishInitialState()  publish m_InitialState once the target canvas is registered (see notes)
HasCanvasForState(state)  true when a non-persistent registered canvas matches the state

Notes:
- Initial-state publishing is gated on "the target canvas for m_InitialState is registered", NOT on "the first canvas registered". LoadingCanvas is persistent and is created at runtime by LoadingController during the UI bundle preload, so it registers BEFORE 1_Content loads; publishing at the first registration then finds a registry holding only persistent canvases, SwitchToState skips those, the target resolves to null, and the initial state was already marked published -> every canvas stays visible and the first screen never starts. TryPublishInitialState therefore retries on each canvas registration until the target arrives, PublishInitialState polls with a 15s backstop, and a publish made with zero subscribers re-arms the flag.
- Persistent canvases (LoadingCanvas) are excluded from the switch table: they are never a switch target and never faded out or ClearPanels-ed, because the loading mask must outlive the whole switch.
- SwitchToState logs a warning when no canvas matches the requested state: that silent return previously hid the startup-ordering bug above.

- The project state must be written before the canvas rebuild: panels may read it in Awake to decide which entries to show.
- CanvasRebuildVersion exists for consumers whose content assembly can finish before the rebuild: a prefab may be instantiated immediately on the task-change event, about 0.3s before the rebuild, and starting a chain earlier means the Start step's UI panel is destroyed by ClearPanels(). StepManager waits on this version through its canvasRebuildWaitTimeout. It is exposed in this manager layer to keep managers from depending on UI's CanvasBase.
- IsSwitching means "do not build panels now": panels created during a switch are removed by ClearPanels.
- Only one switch coroutine runs at a time; a new switch cancels the previous one so animations cannot stack.
- OnTaskTypeChanged rebuilds only in UI/Roaming, so a task change arriving on the start/login/menu pages does not rebuild the current screen.
- GetPanel<T> skips inactive and persistent canvases, so panels are never created under a hidden canvas.
- ⚠️ GetPanel CREATES the panel when it is not registered (CanvasBase.CreatePanel), so any "read only" path must use CanvasBase.FindPanel instead. (The former TaskPanelDescription violated this and spawned phantom panels on every AI context build; it was removed in the 2026-09-30 cleanup.)
