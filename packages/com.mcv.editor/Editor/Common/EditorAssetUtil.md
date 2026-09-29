# Contract: EditorAssetUtil

Role: shared editor-side infrastructure for assets and compile readiness: ensuring asset folders, guarding "script is compiled" before any CreateAsset menu runs, load-or-create ScriptableObject assets, and cleaning residue and broken assets.

Methods:
EnsureFolder(folder)  recursively create an Assets folder (parent first); no-op when it is already a valid folder
IsScriptReady(scriptPath)  true when the MonoScript asset exists and GetClass() resolves; the mandatory first guard of every CreateAsset menu entry
LoadOrCreateAsset<T>(assetPath, logTag, createdLog)  load T; when a same-named asset loads null (m_Script lost) delete then recreate; logging gated by logTag and createdLog
DeleteAssetsNotIn<T>(dir, keepIds, idOf, logTag)  delete assets under dir whose id is not in keepIds (residue and broken null assets); returns the deleted paths
FindBrokenAssets<T>(dir, expectedIds)  read-back self-check; returns the paths of AB_{id}.asset entries that load as null

Notes:
- Assets/Editor/Common is the shared kernel: do not copy these helpers into individual tools.
- The caller owns the side effects: FindBrokenAssets only reports, while the caller deletes and prompts a rerun of the menu.
- LoadOrCreateAsset deletes a broken same-named asset before recreating it, avoiding the "config exists but the package DB shows None" symptom.
- IsScriptReady must gate CreateAsset: during compilation or a domain reload GetClass() is null and the asset would be written with m_Script: {fileID: 0}.
- Path convention: folder and asset paths are AssetDatabase paths, idOf maps an asset to its logical id, and a ScriptableObject class must live in a .cs file named exactly like the class.
- These helpers touch assets and folders only; flushing is left to the callers (for example SaveAssets).
