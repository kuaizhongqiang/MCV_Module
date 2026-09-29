# Contract: PanelGeneratorWindow

Role: editor window that scaffolds a Panel class, a Controller class and a prefab skeleton under Assets/Prefabs/UI/Panels/ (the source folder of the UI global bundle UI/ui).

Fields:
m_PanelName:string  base name input
m_Ownership:int  0 = module package, 1 = host (default), 2 = custom path
m_CustomPath / m_NamespaceOverride / m_ControllerNamespaceOverride:string  custom directory and namespace overrides
m_GenerateController / m_GeneratePrefab:bool  generation toggles
OwnershipLabels:string[]  popup labels
PanelNamespace / ControllerNamespace:string  effective namespaces

Methods:
OpenWindow()  the menu entry that opens the window
OnGUI()  draw the generator UI
CanGenerate(out error) / NormalizeBaseName(name)  validation and base-name normalisation
GetPanelPath(name) / GetControllerPath(name)  resolve the output paths by ownership
Generate()  write the files, refresh and schedule the prefab skeleton
WritePanelFile(...) / WriteControllerFile(...) / CreatePrefabSkeleton(...)  file writers and delayed prefab creation
ResolveType(name)  cross-assembly type lookup

Notes:
- The prefab always lives under Assets/Prefabs/UI/Panels/ (the bundle source folder) regardless of ownership; it must be part of the UI global bundle (UI/ui, id ui_<className>), so MCV Build/UI prefab AB has to be re-run after generating or editing it.
- The window is not bound to any Canvas: a panel is a reusable asset and each CanvasBase subclass calls it from its own state logic.
- CreatePrefabSkeleton runs after compilation finished, because the panel type must exist before the component can be attached.
