# Contract: MenuCanvas

Role: menu-page canvas; the old MenuPanel was removed in the 2026-09-30 cleanup, so rebuild currently only logs.

Methods:
Awake()  base only
OnRebuild()  logs a placeholder line (menu panel to be rewritten)

Notes:
- The canvas does not test the state: the target canvas was already chosen by SceneStateChangeEventData.
- A new menu panel/controller will be written and wired here later.
