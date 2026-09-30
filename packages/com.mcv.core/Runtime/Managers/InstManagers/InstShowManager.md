# Contract: InstShowManager

Role: display-only instance manager; instances exist for viewing only (pose/size handled by the display script) and never take part in step interaction.

Fields:
s_Instance:InstShowManager  static scene singleton, no DontDestroyOnLoad
Exists:bool  static; use during shutdown instead of triggering a lookup

Methods:
Instance  get (FindObjectsByType on demand; never creates a GameObject) / set
AllowEmptyPackageKeys  override = true
Awake()  duplicate -> warn + Destroy(gameObject); else claim s_Instance, isControlled = false, base.Awake()
OnDestroy()  base + clear s_Instance when it is this
Show(packageId, parent=null)  static; Spawn through Instance; null when the manager or the package is missing
Hide(instance)  static; Despawn with a display-flavoured name
SetShowRootActive(active)  static; toggle the manager GameObject; false when no manager exists

Notes:
- Pooling, package loading and instance ownership all come from InstManagerBase; this class only adds the scene singleton, isControlled = false, and the static Show/Hide/SetShowRootActive entry points.
- Package keys may be left empty: nothing is preloaded and nothing warns, so a package is loaded only when it is asked for (the 8-device overview page works this way). With packageKeys configured the base preloads and pools, so Spawn resolves synchronously.
- The host is meant to start inactive to save its render camera, but Unity does not call Awake/Start on inactive objects, so the manager stays uninitialised until the first SetShowRootActive(true): Awake -> Start -> DelayInit then preloads and builds the pool.
- Switching the host off only runs OnDisable: loaded bundles, prefab cache and pool all survive, and reopening needs no reload.
- Dies with the 1_Content scene, by design.
