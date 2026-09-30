# Contract: InspectionManager

Role: inspection-task scheduling; assembles the inspection prefab from the current ProjectClip's taskInspectionData.prefabKey and destroys it when the task changes.

Fields:
s_Instance:InspectionManager  static scene singleton
currentModel:GameObject  the assembled inspection instance
pendingClipId:string  clip awaited from ClipReadyEvent
boundClipId:string  clip of the current instance
CurrentModel:GameObject  public view of currentModel
IsModelBound:bool  currentModel != null
BoundClipId:string  public view of boundClipId
AllowEmptyPackageKeys  override = true

Methods:
Instance  get (FindObjectsByType on demand; never creates a GameObject) / set
Exists  static; use during shutdown
Awake()  duplicate -> warn + Destroy(gameObject); else claim s_Instance, base.Awake(), subscribe ClipReadyEvent + TaskTypeChangeEventData + SceneStateChangeEventData
DelayInit()  base.DelayInit (no preload) -> BindCurrentClip() when the current task is already Inspection
OnDestroy()  unsubscribe the three -> AbandonModel() -> base -> clear s_Instance when it is this
OnClipReady(e)  bind only when e.ClipId == pendingClipId and the clip is still current
OnTaskTypeChanged(e)  Inspection -> BindCurrentClip(), otherwise ReleaseModel()
OnSceneStateChanged(e)  SceneState.UI -> bind when the task is Inspection; any other state -> AbandonModel()
BindCurrentClip()  clip missing -> warn; clip not ready -> remember pendingClipId and wait for ClipReadyEvent; else Bind
Bind(clip)  ReleaseModel -> read taskInspectionData.prefabKey -> GetPrefabByPackageId -> InstantiatePrefab -> record currentModel/boundClipId
ResolveModelParent()  objParent when configured, else this transform
ReleaseModel()  clear ids -> ReleaseInstance (Destroy as a fallback) -> currentModel = null
AbandonModel()  clear ids and currentModel without destroying

Notes:
- The inspector prefab is deliberately not pooled: probes get dragged and points keep contact state, which a reused instance would carry over; re-instantiating is the reliable reset (same as TaskStructureController for structure models).
- Resources are not loaded here: content packages are installed and removed by GlobalAssetsMgr per currentClip, and this class only does "package ready -> take prefab by key -> instantiate -> release" (convention: Docs/design_ai/BundlePipeline.md section 6).
- Instances go through GlobalAddressableMgr.InstantiatePrefab so the instance-to-package registration exists and unloading the content package destroys them.
- Leaving the content page (SceneState != UI) must AbandonModel rather than ReleaseModel: the package unload is already destroying the instance and ReleaseInstance would hit a dead registration.
- Every entry into the task builds a brand-new instance.
