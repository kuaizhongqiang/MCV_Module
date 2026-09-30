# Contract: UIPrefabUtil

Role: the single entry point for fetching a UI prefab out of the UI global bundle (`UI/ui`); maps a prefab name to a package-config id and returns the already-preloaded asset.

Methods:
Get(prefabName)  sync fetch of an already-preloaded UI prefab by name; returns null and logs an Error when it is not ready (never triggers a load)

Notes:
- The fetch is deliberately cache-only: panel and fragment creation is synchronous (CanvasBase.CreatePanel must return a panel from inside Rebuild), so the UI bundle has to be preloaded during Setup; a load-on-demand path would leave the call sites empty-handed.
- The id comes from ContentNaming.UIPrefabId (`ui_<prefabName>`) — the same rule the Editor pipeline uses when it writes the package configs; never hardcode the id at a call site.
- These prefabs used to be loaded with `Resources.Load("UI/<name>")` and moved into the bundle in B1.5; passing the old `UI/...` path here returns null.
