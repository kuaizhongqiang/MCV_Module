# Contract: TaskListPanel

Role: step list panel (View); builds the task step navigation and reports task-type switches. Display only, the current type is injected by the controller.

Fields:
taskToggleParent:Transform  parent whose child order defines the step items (required)
selectedColor / normalColor:Color  highlight colors of the mask image
currentProjectClip:ProjectClip  bound project data
m_CurrentTaskType:TaskType  current displayed task type (injected; used for display and de-duplication)
m_Items:List<StepItem>  step items, index-aligned with GetActiveTasks()
hideYFloat:float  anchored Y when hidden (-130)
isActiveNow:bool  initial visible state
m_TargetActive:bool  last requested state, used to debounce duplicate SetUIActive

Methods:
Awake()  collect StepItems from taskToggleParent, cache the mask, apply the initial state
FindMask(toggle) / FindMask(button)  locate the highlight image of a Toggle / Button row (author-supplied)
Init(project, taskType)  build the step items, check the current one, bind listeners (active tasks only)
GetActiveTasks()  enabled tasks (taskActive == false filtered out)
BindItem(item, type) / UnbindItem(item)  bind and clear the row listeners
SelectTask(type)  same as the current type -> ignore; else publish TaskTypeChangeEventData
SetItemSelected(index, isOn)  display-only selected state
SetTaskType(taskType)  refresh the whole list display
SetUIActive(isActive) / SetUIActiveImmediately(isActive)  override; debounced slide/fade / instant apply
ActiveState(isActive) / OverrideAnimCoroutine(isActive)  set alpha, interactable and the list Y / lerp them

Nested type StepItem  one step row; supports both Toggle and Button art and is assembled by child order only
Fields:
toggle:Toggle  the row's toggle (or null)
button:Button  the row's button (or null)
mask:Image  highlight image, cached in Awake
Go:GameObject  the toggle's or button's gameObject
Methods:
FindMask(toggle) / FindMask(button)  locate the highlight image in the prefab

Notes:
- Init and SetTaskType must use the same "active tasks" filter, otherwise step items and tasks misalign.
- The m_TargetActive debounce is required, otherwise repeated SetUIActive flickers alpha 0-1-0.
- FindMask may return null; SetItemSelected then falls back to UGUI's selected state, so clicking still works.
- Step items are gathered once in Awake (child order); the panel never re-scans the hierarchy afterwards.
- Selecting an item restores the current task on re-init without firing a switch (display and logic are separate).
