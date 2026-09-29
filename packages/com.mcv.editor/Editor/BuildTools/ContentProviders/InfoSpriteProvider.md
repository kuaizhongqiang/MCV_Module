# Contract: InfoSpriteProvider

Role: IContentProvider collecting the intro sprite atlas of a clip from Assets/Sprites/Origin/Content/Info/{index}{deviceName}/*.png (six per device), pairing the on-disk images with the JSON image id list.

Fields:
InfoRoot:const string  "Assets/Sprites/Origin/Content/Info": the sprite source root

Methods:
Name  "InfoSprite", the provider source name used in reconciliation reports
Collect(clip, device)  read taskInfoData.images through clip.GetTaskData<TaskInfoData>(TaskType.Info), find the clip folder by displayName, list the *.png files recursively (excluding .meta, case-insensitive sort), pair files positionally with the JSON ids and emit entries: Error for a missing asset or a naming mismatch, isExtra for an unreferenced asset; id = ContentNaming.InfoSpriteId(device, i + 1)
FindClipFolder(clip)  first sub-directory of InfoRoot whose name contains clip.displayName; null when the clip, displayName or InfoRoot is missing

Notes:
- Assets/Editor/Common is the shared kernel; providers are thin data adapters and must not duplicate path or naming helpers.
- Naming convention: id = {device}_info_{NN} built only through ContentNaming, while the folder name is {index}{Chinese device name}.
- JSON (taskInfoData.images) is the source of truth: pairing is positional, a required but missing asset yields an empty assetPath with isError, and an asset not referenced by JSON yields isExtra.
- Read the clip through the public GetTaskData<T>() / GetTask<T>(), because its taskXxxData serialized fields are private and unreadable from the Editor assembly.
- Asset paths use forward slashes and comparisons are case-insensitive.
- Read-only: no disk writes and no AssetDatabase refresh here.
