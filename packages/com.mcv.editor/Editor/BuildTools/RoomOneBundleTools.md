# Contract: RoomOneBundleTools

Role: the menu entries for the RoomOne global bundle (bundle RoomOne/roomone); the pipeline itself lives in Assets/Editor/Common/GlobalBundleRunner and the data lives in BuildTools/GlobalProviders/RoomOneGlobalProvider.cs.

Fields:
Provider:static readonly IGlobalBundleProvider  the RoomOneGlobalProvider singleton this tool drives

Methods:
BuildMenu()  [MenuItem("MCV Build/RoomOne 房间图标 AB（九张打成一包）", priority 64)] generate the configs and build
ConfigOnlyMenu()  [MenuItem("MCV Build/内容 AB/RoomOne 仅生成配置（不构建）", priority 85)] generate the configs only, without building
Run(build, interactive)  non-interactive core; delegates straight to GlobalBundleRunner.Run(Provider, build, interactive), and interactive=false shows no dialog at all

Notes:
- The eight pipeline segments (compile guard, asset validation, stale-config sweep, config writing, read-back self-check, PackageDB sync, dialogs, building) used to live here and were almost line-by-line identical to CameraBgBundleTools; they all moved into GlobalBundleRunner in B1.5, so this file is only two menus plus a provider reference.
- Behaviour is unchanged: same two menu texts and priorities, same config directory, same ids, same bundle name and output directory, same log tag [RoomOneAB] and dialog title, the same stale-config sweep semantics, and the same sweepStale build.
- The icon table (eight id / path pairs, Pic-xcy.png excluded) is preserved verbatim in RoomOneGlobalProvider, together with the "file name must match character for character" hint that is now carried by the entry note.
- This is a global bundle rather than a content bundle: ABPackageConfigSO.clipId is left empty and GlobalAddressableMgr.GetConfigsByClip returns an empty table for it, which keeps the per-clip load and unload chain away.
- The output directory StreamingAssets/RoomOne/ is deliberately separate from Content/ so the content pipeline's sweepStale can never delete it, and vice versa.
- Assets must be imported as Sprite, otherwise the runtime's Sprite load returns null.
