# Contract: RoamingCanvas

Role: roaming-page canvas; on rebuild it assembles the roaming function panel and, when AI is on, the AI dialog panel.

Methods:
Awake()  base only
OnRebuild()  AiDialogPanel when GlobalAiMgr.Instance.IsAiEnabled, then GetPanel<RoamingFunctionPanel>
CreatePanelByTaskType()  switch on the current task type per task panel

Notes:
- The canvas does not test the state: the target canvas was already chosen by SceneStateChangeEventData.
- CreatePanelByTaskType currently has no caller: the roaming page builds the function panel, and the task panels are owned by ContentCanvas.
