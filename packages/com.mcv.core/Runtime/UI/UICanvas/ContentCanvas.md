# Contract: ContentCanvas

Role: content-page canvas; on rebuild it assembles the function panel, the task list, the task panel matching the current task type and, when AI is on, the AI dialog panel.

Methods:
Awake()  base only
OnRebuild()  GetPanel<ContentFunctionPanel> + GetPanel<TaskListPanel> + CreatePanelByTaskType() + AiDialogPanel when GlobalAiMgr.Instance.IsAiEnabled
CreatePanelByTaskType()  switch on GlobalDataMgr.GetCurrentTaskType(); LineConnection / Training / Inspection also pull TipsPanel

Notes:
- The canvas does not test the state: the target canvas was already chosen by SceneStateChangeEventData.
- The task type is never passed as a parameter; it is read from the single source written by TaskTypeChangeEventData (GlobalUIMgr.OnTaskTypeChanged).
- Purpose / Equipment / Principle / LineConnection / Training / Test / Info / Structure / Inspection / Exam each map to their own panel; unlisted types build nothing extra.
- The function panel and the task list are built first, so the task panel is created on top of them.
