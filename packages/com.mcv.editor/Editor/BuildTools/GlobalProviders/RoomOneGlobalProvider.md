# Contract: RoomOneGlobalProvider (+ IconEntry)

Role: IGlobalBundleProvider for the RoomOne roaming-room HUD icons: eight icons packed into one global bundle (bundle RoomOne/roomone); the whole flow lives in Assets/Editor/Common/GlobalBundleRunner.

Fields:
IconRoot:const string  "Assets/Sprites/Origin/Menu": the icon source directory
FileNameHint:const string  the parenthetical hint appended to a missing-asset problem ("表中文件名与工程实际文件名（含大小写）必须逐字一致")
Icons:IconEntry[]  the eight room icon entries
IconEntry.Device:string  device segment, clip.id without the "clip_" prefix
IconEntry.FileName:string  the actual project file name, whose casing is inconsistent on purpose
IconEntry.Id:string  ContentNaming.RoomIconIdOfDevice(Device), i.e. roomone_{device}
IconEntry.AssetPath:string  IconRoot + "/" + FileName

Methods:
Name  "RoomOne", the provider source name (log tag [RoomOneAB], dialog title "RoomOne AB")
MenuPath  "MCV Build/RoomOne 房间图标 AB（九张打成一包）", the menu literal owned by RoomOneBundleTools
ConfigDir  EditorPaths.RoomOneConfigDir
BundleName  ContentNaming.RoomOneBundleName (RoomOne/roomone)
CleanStaleConfigs  true: a dropped icon entry leaves a stale config that must be deleted, otherwise AutoCollect pulls the unresolvable id back into the master list
AbortOnFirstAssetProblem  false: a fixed table can report every problem at once
BuildWith  empty: the icons only reference in-project sprites, so nothing has to be built in the same BuildAssetBundles call
Collect()  emit the eight entries with kind Sprite and FileNameHint as note

Notes:
- This is a global bundle rather than a content bundle: the eight icons enter and leave the room together, so ABPackageConfigSO.clipId is left empty (the runner writes null) and GlobalAddressableMgr.GetConfigsByClip returns an empty table for it, which keeps the per-clip load and unload chain away.
- The config id is roomone_{device} derived through ContentNaming, so the runtime resolves the icon for a clip with the same method and needs no second mapping table.
- The output directory StreamingAssets/RoomOne/ is deliberately separate from Content/ so the content pipeline's sweepStale can never delete it, and vice versa.
- The icon table reflects the actual project file names (casing is not uniform); edit the table and rerun the menu to change an icon or its ownership, and the stale config of a removed entry is swept automatically.
- Room 1 has no quiz HUD, so the unused Pic-xcy.png is intentionally excluded: shipping it would waste space and leave an unused config.
- Assets must be imported as Sprite, otherwise the runtime's Sprite load returns null.
- BuildWith is empty because the eight icons reference no other global bundle, so a separate build copies nobody's assets; RoomOne may be built on its own.
- This file carries data only; never re-implement guard, validation, config writing, DB sync or building here.
