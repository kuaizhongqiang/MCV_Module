# Contract: MenuCanvas

Role: menu-page canvas; its rebuild now creates MenuPanel (the old panel was removed in the 2026-09-30 cleanup, a new skeleton was generated on 2026-10-08).

Methods:
Awake()  base only
OnRebuild()  log the rebuild, then `GetPanel<MenuPanel>()` (creates the panel when the canvas has none) and log the created panel's name

Notes:
- The canvas does not test the state: the target canvas was already chosen by SceneStateChangeEventData.
- `GetPanel<T>()` creates the panel (CanvasBase.CreatePanel); every read-only / describe path must use FindPanel<T>() instead, otherwise a ghost panel is instantiated.
- The panel is still a skeleton (see `MenuPanel.md`): rebuild only instantiates it and logs, no menu content is assembled yet.
