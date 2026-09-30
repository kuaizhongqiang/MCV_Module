# Contract: TaskPanelBase

Role: base for task panels; adds the content contract that GlobalUIMgr reads when building the AI context.

Methods:
GetPanelContent()  abstract; returns the panel's text content

Notes:
- Only task panels the AI context cares about derive from this class: TaskExamPanel does not, and GlobalUIMgr.TaskPanelDescription uses fixed text for Exam because calling GetPanel there would instantiate the panel as a side effect.
