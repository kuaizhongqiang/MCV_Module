# Contract: EditorPaths

Role: single source of truth for the editor toolchain's project path constants: content AB input, package DB master, config directories, AB output root and directory segments, and the temp staging root.

Fields:
ProjectDataJson:const string  "Assets/StreamingAssets/Data/ProjectData.json": the content-AB input, read at runtime through JsonReaderWriter.FULL_PATH
PackageDbAsset:const string  "Assets/Resources/Config/PackageDB_Master.asset": the package DB master (AutoCollect target), loaded at runtime via Resources.Load
AbConfigScript:const string  "Assets/Scripts/Models/Addressable/ABPackageConfigSO.cs": the script the compile-readiness guard watches
ContentConfigDir / CameraBgConfigDir / RoomOneConfigDir / UIConfigDir / FontConfigDir:const string  content, global (non-content), RoomOne icon, UI global-bundle (B1.5) and font global-bundle (B3) package config directories
UIPanelPrefabDir / UIFragmentPrefabDir:const string  "Assets/Prefabs/UI/Panels" / "Assets/Prefabs/UI/Fragments": the B1.5 UI bundle source prefab directories, scanned at run time by UIPrefabGlobalProvider
FontAssetDir:const string  "Assets/Fonts": where the legacy .ttf and TMP SDF assets live; documentation / manual cross-check only, the entry paths come from FontCatalogSO
StreamingAssetsRoot:const string  "Assets/StreamingAssets": the AB output root
ContentBundleDirName / RoomOneBundleDirName / UIBundleDirName / FontBundleDirName:const string  bundle output directory segments referencing ContentNaming
CameraBgBundleDirName:const string  "CameraBg"
LegacyConfigDirs / LegacyBundleDirs:static readonly string[]  deprecated P1 directories, verified absent

Methods:
TempRoot(name)  absolute Temp staging directory Temp/MCV_{name}, never imported into the project

Notes:
- Assets/Editor/Common is the shared kernel: do not copy these path literals into other files.
- Only currently referenced paths live here; do not pre-list future paths.
- The bundle directory segments reference ContentNaming so the runtime URL assembly and the tool's disk writes share one constant.
- Temp staging sits under Temp/MCV_{name} (absolute and not imported); only the bundle body leaves Temp into StreamingAssets.
- Every literal path value is contract and must not change, because both the runtime and the tooling resolve their files from them.
- B1.5 added the UI global-bundle paths (UIConfigDir, UIPanelPrefabDir, UIFragmentPrefabDir, UIBundleDirName); the two prefab directories are inputs only, so they may not exist yet while the prefab migration is in flight, and the provider must warn and skip rather than fail.
- B3 added the font global-bundle paths (FontConfigDir, FontAssetDir, FontBundleDirName) following the same convention: the output directory segment references ContentNaming rather than a literal, and FontAssetDir is documentation only because the provider reads each asset path from FontCatalogSO instead of scanning the folder (adding a font is then a data-only change).
