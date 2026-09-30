# Contract: LoadingController

Role: loading-mask coordinator; shows the mask on SceneLoadingEvent, drives progress, and hides it on SceneLoadedEvent while honouring a minimum show duration.

Fields:
minShowDuration:float  [SerializeField] minimum mask show time in seconds, 1.2
m_IsLoading:bool  the mask should be shown (from a session's first event until the real hide)
m_LastProgress:float  last progress, used to restore the panel after a rebuild
m_ShowStartTime:float  unscaledTime start of the current show phase
m_SessionId:int  loading session counter (+1 per new loading); the delay-hide guard
m_DelayHideCoroutine:Coroutine  pending delay-hide coroutine

Methods:
Awake()  base -> subscribe SceneLoadingEvent / SceneLoadedEvent
OnViewBound()  restore panel visibility to m_IsLoading and its last progress
ResolvePanel(createIfMissing)  bound View when alive; else LoadingCanvas.Ensure/Find -> GetPanel/FindPanel<LoadingPanel>
OnSceneLoading(e)  CancelDelayHide -> new session (+1, start time) -> mark loading and progress -> resolve or create the panel -> show with the generic mask text on session start (through Lang.Get: ui.loading.title / ui.loading.content) -> SetProgress
OnSceneLoaded(e)  resolve the existing panel; gone or inactive -> clear state; else when the remaining minimum time > 0 -> DelayHideRoutine(sessionId, remain), else HidePanel
DelayHideRoutine(sessionId, delay)  WaitForSecondsRealtime -> keep only when sessionId == m_SessionId -> HidePanel(ResolvePanel(false))
CancelDelayHide()  stop and clear m_DelayHideCoroutine
HidePanel(panel)  clear loading state -> inactive guard -> SetUIActive(false)
OnDestroy()  CancelDelayHide -> unsubscribe both events -> base

Notes:
- The mask lives on the persistent LoadingCanvas (IsPersistent + sortingOrder 1000), not on a state canvas: a state canvas is faded out and ClearPanels-ed on every switch, which would rip the mask away and flicker when loading is fast.
- Because of that, never rely only on the ControllerBase-bound View; lazy-load from the persistent canvas when needed.
- The minimum show duration uses unscaledTime so pause and timeScale = 0 still work; when the real load is slower, hide immediately with no extra wait.
- The delay-hide guard must be m_SessionId, never m_IsLoading: within the window m_IsLoading stays true on purpose, so using it would make the delay-hide always self-abort and the mask never unload.
- The hide path must never create a panel (createIfMissing = false): a fresh instance's Awake would light one frame and flicker again.
- A new loading must cancel the previous delay-hide, otherwise it fires mid-session and closes the panel right after it appeared.
- The mask texts must go through Lang.Get (both keys are registered in LanguageDataSO): a hard-coded Chinese literal here bypasses the key system and leaves this block Chinese forever in the English build.
