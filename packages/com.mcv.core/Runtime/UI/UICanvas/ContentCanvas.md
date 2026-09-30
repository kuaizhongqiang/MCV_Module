# Contract: ContentCanvas

Role: content-page canvas; business panels were removed in the 2026-09-30 cleanup, so rebuild only attaches the AI dialog panel when AI is on.

Methods:
Awake()  base only
OnRebuild()  GetPanel<AiDialogPanel> when GlobalAiMgr.Instance.IsAiEnabled

Notes:
- The canvas does not test the state: the target canvas was already chosen by SceneStateChangeEventData.
- 2026-09-30 cleanup: the function panel / task list / per-task-type task panels (ContentFunctionPanel, TaskListPanel, Task*Panel, TipsPanel) were removed together with the module-side business controllers; this canvas is a skeleton until the content page is rewritten.
