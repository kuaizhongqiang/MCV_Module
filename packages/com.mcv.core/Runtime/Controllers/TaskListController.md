# Contract: TaskListController

Role: task-list controller; assembles the task list after each panel rebind and refreshes the display on task switch.

Methods:
Awake()  base -> subscribe TaskTypeChangeEventData (resident controller, subscribe once; EventBus de-dups the subscription)
OnDestroy()  unsubscribe -> base
OnViewBound()  View.Init(project, GlobalDataMgr.GetCurrentTaskType()) when a ProjectClip exists
OnTaskChanged(e)  View.SetTaskType(e.TaskType) (display only)

Notes:
- The current task type has a single writer: GlobalUIMgr writes ProjectData while handling TaskTypeChangeEventData; this controller never writes it, to avoid two state sources.
- The controller is resident, so the subscription lives in Awake and the unsubscribe in OnDestroy, while OnViewBound re-runs on every panel rebuild.
