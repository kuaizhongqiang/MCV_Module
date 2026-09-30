# Contract: CanvasBase

Role: canvas base; registers itself with GlobalUIMgr, owns the panel registry, and clears then rebuilds its children on every state or task-type change.

Fields:
m_SceneState:SceneState  [SerializeField] the state this canvas serves, UI by default
canvas:Canvas  [RequireComponent] cached in Awake
panels:Dictionary<string, PanelBase>  type name -> panel
CanvasState / MatchesState(state)  read-only state, and the test GlobalUIMgr uses to pick a switch target
IsPersistent:bool  virtual; true = never faded out, hidden, cleared or chosen as a target

Methods:
Awake()  base.Awake -> cache Canvas, ClearChildren, GlobalUIMgr.RegisterCanvas
OnDestroy()  base.OnDestroy -> UnregisterCanvas while GlobalUIMgr still exists
ClearPanels()  destroy every child and clear the registry; the Canvas itself survives
Rebuild()  ClearPanels + OnRebuild
OnRebuild()  virtual; subclass builds its panels here
RegisterPanel(panel) / UnregisterPanel(panel)  registry by type name; registration also calls panel.SetCanvas(this)
GetPanel<T>()  the registered panel, or create it from the UI global package (UIPrefabUtil.Get) when missing
FindPanel<T>()  the registered panel only, null when absent, never creates
CreatePanel(panelName)  UIPrefabUtil.Get(panelName), a sync cache read of the UI global package by id ui_{class name} -> instantiate under this canvas, register; prefab null -> null
CreatePanel<T>()  protected helper for OnRebuild
LayoutRebuild()  force-update canvases and rebuild the root layout immediately

Notes:
- Persistent canvases (LoadingCanvas) must outlive a state switch: they are excluded from fading, ClearPanels and target selection, otherwise a fast load would flicker when the mask is torn off.
- FindPanel exists because GetPanel would create a missing panel just to close it again, and a fresh instance flashes one frame through Awake's isActiveOnInstance.
- Rebuild takes no state or taskType argument: the target canvas comes from the event and the task type from GlobalDataMgr.GetCurrentTaskType, so neither is threaded through as a parameter.
- The panel prefab fetch is synchronous, so it depends on Setup having preloaded the whole UI package; UIPrefabUtil logs the Error and returns null when the prefab is not ready (CanvasBase no longer logs it itself).
