# Contract: TaskStructureController

Role: structure-panel scheduler; loads the 3D exploded model by the current ProjectClip's taskStructureData.prefabKey, drives its InstControlledManager (manual click-through, then auto pingpong) and reports the structure score.

Fields:
initObjParent:Transform  [Tooltip] parent of the model instance; its parent (the show root) is closed by default
autoPingPongSpeed:float  [SerializeField] 2; pingpong speed, must be positive
pendingClipId:string  clip id recorded while the bundle is not ready; played on ClipReadyEvent
structureModel:GameObject  current model instance (destroyed or abandoned on teardown)
currentObjMgr:InstControlledManager  step system on the current model
playingClipId:string  clip id of this run, used to claim StructInteractiveCompletedEvent

Methods:
Awake()  base -> subscribe ClipReadyEvent, TaskTypeChangeEventData, SceneStateChangeEventData, StructInteractiveCompletedEvent, GlobalInteractionEventData; logs an error when initObjParent is null
OnDestroy()  unsubscribe all -> AbandonModel -> base
OnViewBound()  clip null -> warn and skip; IsClipReady ? pendingClipId = null and BindAndPlay : record pendingClipId and wait
OnClipReady(e)  only its own pendingClipId -> clear -> re-fetch the clip -> BindAndPlay
OnTaskTypeChanged(e)  non-Structure task -> ReleaseModel
OnSceneStateChanged(e)  non-UI state -> AbandonModel (no pool return)
OnStructStepsCompleted(e)  only its own playingClipId -> ReportStructureScore -> View.CloseTips -> PlayAutoPingPong(-autoPingPongSpeed)
ReportStructureScore(clip)  GetTaskData(Structure) -> ReportScoredUnit(..., completed: true)
OnGlobalInteraction(e)  Enter on a HoverOrTips StructureTaskObj -> View.ShowTips(name); Exit -> View.CloseTips
BindAndPlay(clip)  ReleaseModel(false) -> prefabKey -> GlobalAssetsMgr.GetPrefabByPackageId -> SetShowRootActive(true) -> InstantiatePrefab -> StartSteps
PlayAutoPingPong(speed)  PlayAuto(speed, callback flips the sign); the chain breaks once currentObjMgr is nulled
ReleaseModel(closeRoot)  StopPlayback -> ReleaseInstance (Destroy fallback) -> closeRoot ? SetShowRootActive(false)
StopPlayback()  View.CloseTips -> StopSteps -> playingClipId = null
AbandonModel()  StopPlayback -> drop the reference -> SetShowRootActive(false)
SetShowRootActive(active)  toggle initObjParent.parent activeSelf

Notes:
- The model is never pooled: the structure animation mutates part transforms and Animation.Stop() does not reset the pose, so a fresh Instantiate is the only reliable reset; teardown destroys it.
- Open the show root before instantiating: under an inactive root Awake/Start is deferred, InstControlledManager is not registered and the following StartSteps no-ops.
- The bundle is loaded and unloaded by GlobalAssetsMgr per currentClip, and OnViewBound runs before loading finishes, so the ready/pending two-step is required.
- Two teardown paths: in-page task switch -> ReleaseInstance (destroy, so the next visit is fresh); leaving the content page -> AbandonModel only (GlobalAssetsMgr unloads the bundle and destroys the instance, so Despawn would hit a dead pool).
- Pingpong starts negative (reverse) on purpose: the manual steps just took the parts apart, so reversing plays them back; starting positive would double up two forward runs. autoPingPongSpeed must be positive.
- PlayAuto deliberately never publishes StructInteractiveCompletedEvent (that signal means "the manual run finished once"), so "manually clicked every part" is the only completion criterion.
- Hover is controller-driven: the panel does not know scene objects; only StructureTaskObj with HoverOrTips on is shown, and CloseTips is called manually when switching to auto because the colliders get disabled and no Exit event arrives.
- ReleaseModel's closeRoot flag: true on task-switch teardown, false before re-binding (the root is reopened right after).
