# Contract: TaskDefaultPanel

Role: default task panel (View); no content of its own, only a placeholder snapshot.

Methods:
GetPanelContent()  returns the placeholder text "任务默认面板"

Notes:
- No creation site any more: GlobalUIMgr.TaskPanelDescription used to instantiate it through GetPanel for the task types that fell into its default branch (that is what made a phantom TaskDefaultPanel appear), and the fix switched that path to FindPanel plus an empty fallback. ContentCanvas.CreatePanelByTaskType never had a case for it either.
- Kept as the minimal template for task panels (and its prefab still ships inside the UI/ui global bundle).
