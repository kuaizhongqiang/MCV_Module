# Contract: TaskInfoController

Role: info (intro) panel controller; assembles the intro picture set, the copy text and the component model from the current ProjectClip's taskInfoData, and resets the display camera whenever the panel opens.

Fields:
pendingClipId:string  clip id waiting for its package (recorded in OnViewBound, assembled on ClipReadyEvent)
showObjMgr:InstShowManager  display manager used for this assembly (its host object is the show root ShowObjParent)
showModel:GameObject  currently shown model instance (returned on component switch or exit)

Methods:
Awake()  base -> subscribe ClipReadyEvent, TaskTypeChangeEventData, SceneStateChangeEventData (resident controller, subscribe once)
OnDestroy()  unsubscribe all three -> ReturnModel -> base
OnViewBound()  ResetCameraPose first -> clip null warns; package ready ? BindAll : record pendingClipId and wait for ClipReadyEvent
ResetCameraPose()  FocusRotationControl.ResetPos() instant snap; warns when the controller is missing
OnClipReady(e)  assemble only for the clip this controller is waiting on (pendingClipId)
OnTaskTypeChanged(e)  non-Info task -> HideShowObj
OnSceneStateChanged(e)  non-UI state (back to menu / enter roaming) -> HideShowObj
BindAll(clip)  BindPictureSet + BindShowObj
BindPictureSet(clip, data, textIndex)  GetSpritesByPackageIds -> View.Init(sprites, textIndex); an empty or short set warns
BindShowObj(clip, data)  ReturnModel -> validate prefabKey -> SetShowRootActive(true) -> SpawnAsync into the manager's own parent
ReturnModel()  Despawn the current instance (pool only; keeps the show root and the loaded package)
HideShowObj()  ReturnModel -> SetShowRootActive(false)

Notes:
- Resources are not loaded here: GlobalAssetsMgr loads and unloads the content package when entering the content page, and the panel's OnViewBound always runs before loading finishes, so an unready clip must be remembered and assembled on ClipReadyEvent (BundlePipeline section 6).
- The show root needs both exit paths: TaskTypeChangeEventData only fires while switching steps inside the content page, so leaving to menu or roaming must be caught on SceneStateChangeEventData, otherwise ShowObjParent stays active and its render cameras keep drawing.
- ResetCameraPose must run before every early return (it is the first statement), or an unready package would skip the reset.
- Use ResetPos(), not ResetPosSmooth(), which eases over resetDuration seconds.
- The reset relies on the Canvas rebuilding the panel: every open is a fresh instance that runs Start -> Bind -> OnViewBound once.
- The model goes through InstShowManager's pool, so a returned instance is reused rather than destroyed.
