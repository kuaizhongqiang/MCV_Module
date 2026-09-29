# Contract: PackageConfigWriter

Role: the single write point for how a package config (ABPackageConfigSO) is generated and updated: load-or-create {configDir}/AB_{id}.asset and fill its fields uniformly.

Methods:
CreateOrUpdate(configDir, id, logTag, fill)  ensure the folder, load-or-create the asset, apply fill and SetDirty; returns null for empty arguments or a creation failure
ApplyContent(config, id, clipId, bundleName, assetPath, kind)  unified field fill: sourceAsset forced null, assetKind always set explicitly, variant cleared, displayName from the file name without extension, clipId null for non-content resources such as CameraBg

Notes:
- Assets/Editor/Common is the shared kernel: never fork a second config-writing copy into per-tool code (the two copies had 20 identical lines out of 29 and could diverge independently).
- Two immutable semantics: sourceAsset MUST be null, because config assets live under Resources/ and a reference would pull the asset into resources.assets and double it; and assetKind MUST be written explicitly, because the runtime picks the generic loader from it and the field default differs between Provider and SO.
- Path convention: the config path is {configDir}/AB_{id}.asset, where configDir comes from EditorPaths.
- Write timing: only SetDirty is called here; the disk flush happens later in PackageDatabaseSync.Sync (SetDirty plus SaveAssets), so this writer never calls SaveAssets.
- logTag prefixes debug output and is forwarded to LoadOrCreateAsset.
