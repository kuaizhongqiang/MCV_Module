# Contract: ModelPrefabProvider

Role: generic IContentProvider for the "one TaskData to one prefab" pattern (info, structure, inspection): the id comes from taskXxxData.prefabKey and the asset path is {modelRoot}/{id}.prefab.

Fields:
name:readonly string  source name shown in reconciliation reports
modelRoot:readonly string  prefab source directory
taskSegment:readonly string  task segment constant (ContentNaming.TaskInfo / TaskStructure / TaskInspection)
keySelector:readonly Func<ProjectClip,string>  delegate reading the task's prefabKey from a clip through the public GetTaskData<T>

Methods:
ModelPrefabProvider(name, modelRoot, taskSegment, keySelector)  injects the source name, model root, task segment and key selector
Name  the provider source name used in reconciliation reports
Collect(clip, device)  read the id through keySelector; an empty id means the clip does not need this model and is skipped silently; when the id differs from ContentNaming.ConfigId(device, taskSegment, ResourceModel) an Error entry is emitted, otherwise the prefab is emitted when it exists and an Error entry when it does not

Notes:
- Assets/Editor/Common is the shared kernel; add a new model kind with one new line in ContentBundleTools.Providers rather than creating a new class.
- Naming convention: the id must equal ContentNaming.ConfigId(device, taskSegment, ResourceModel) and the prefab file name must match the id character for character.
- JSON is the only source: an empty prefabKey means "this clip does not need the model" and is skipped silently.
- Read prefabKey through the public GetTaskData<T>() / GetTask<T>(), because the serialized fields are private and unreadable from the Editor assembly.
- Asset paths use forward slashes and existence is checked with File.Exists.
- Read-only: no disk writes and no AssetDatabase refresh here.
