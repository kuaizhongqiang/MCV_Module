# Contract: ProjectData (+ MenuData / MenuClip / ProjectClip / TaskDataBase / TaskData<T> / task data subclasses)

Role: project root data container; holds the clip list and the current selection, and aggregates every task data entry.

Fields:
clips:List<ProjectClip>  the project clip list
currentClip:ProjectClip  [NonSerialized] currently selected clip, default null
currentTaskType:TaskType  [NonSerialized] current task type, default None
projectState:ProjectState  [NonSerialized] project state, default Start
MenuClip.parentId:string  parent menu id, for building structural data
MenuClip.projectId:string  bound project id (looked up in ProjectData.clips, single source); falls back to the clip reference when empty
MenuClip.clip:ProjectClip  bound project data (legacy field, may be null)
TaskDataBase.taskActive:bool  whether the task is enabled; JSON-configurable, false keeps the data but keeps it out of TaskListPanel
TaskInfoData.prefabKey  the intro model config id (e.g. contactor_info_model); empty means images and text only
TaskInfoData.images:List<string>  the ordered intro image config ids (e.g. contactor_info_01)

Methods:
ProjectDescription()  concatenate every clip description
MenuData.GetRootClip() / GetChildClips(parent) / GetChildClips(parentId) / GetParentClip(child) / GetParentClip(childId) / GetClip(clipId) / GetClipIndex(...) / HasChildren(clip)  menu lookups; missing entries return null or -1
MenuData.GetClipDescription()  JSON description of the menu hierarchy (flat clips restored into an id / parentId tree)
ProjectClip.Tasks  the task list in TaskListPanel assembly order (four steps first, the legacy experiment line last)
TaskDataConverter / ProjectClip.GetTaskData  dispatch by TaskType over the same field set

Notes:
- Adding a task type requires updating the enum and every switch (ProjectClip.Tasks, GetTaskData, TaskDataConverter), otherwise the entry is silently dropped.
- taskActive must stay public to be serialized by Newtonsoft: a protected field never reaches the JSON.
- The intro main-image key is kept only so a key already written in JSON deserializes instead of being silently ignored; nothing consumes it.
- The project state and current task type are the single-source runtime fields other systems read through GlobalDataMgr.
