# Contract: RoamingCanvas

Role: roaming-page canvas; business panels were removed in the 2026-09-30 cleanup, so rebuild only attaches the AI dialog panel when AI is on.

Methods:
Awake()  base only
OnRebuild()  GetPanel<AiDialogPanel> when GlobalAiMgr.Instance.IsAiEnabled

Notes:
- The canvas does not test the state: the target canvas was already chosen by SceneStateChangeEventData.
- 2026-09-30 cleanup: RoamingFunctionPanel and the per-task-type panels were removed; the canvas is a skeleton until the roaming page is rewritten.
