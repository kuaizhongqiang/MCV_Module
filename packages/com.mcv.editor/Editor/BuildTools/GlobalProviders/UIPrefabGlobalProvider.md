# Contract: UIPrefabGlobalProvider

Role: IGlobalBundleProvider for the UI global bundle (B1.5): every prefab directly under Assets/Prefabs/UI/Panels and Assets/Prefabs/UI/Fragments goes into one bundle (bundle UI/ui), so the 40 UI prefabs move out of Resources and into the AB pipeline; the whole flow lives in Assets/Editor/Common/GlobalBundleRunner.

Methods:
Name  "UI", the provider source name (log tag [UIAB], dialog title "UI AB")
MenuPath  "MCV Build/UI prefab AB", the menu literal owned by the Build() entry in this file
ConfigDir  EditorPaths.UIConfigDir (Assets/Resources/Config/UIPackages)
BundleName  ContentNaming.UIBundleName (UI/ui; output directory Assets/StreamingAssets/UI/)
CleanStaleConfigs  true: a renamed or deleted prefab would otherwise leave a config whose id resolves to nothing
AbortOnFirstAssetProblem  false: a directory scan reports every problem at once
BuildWith  { FontGlobalProvider } (bundle Fonts/font): the 40 prefabs and their nested base prefabs reference SIMHEI.TTF, so the font bundle must be built in the same BuildAssetBundles call
Collect()  scan both directories (panels first, then fragments) and emit one entry per prefab, id = ContentNaming.UIPrefabId(file name), kind = Prefab
CollectDir(dir, label, list)  private: validate the folder, keep only direct-child prefabs, sort by path, log the count, warn and skip for a missing or empty folder
IsDirectChild(path, dir)  private: compare the parent directory with a forward-slash, case-insensitive comparison
Build()  static [MenuItem] entry, interactive run (confirm dialog before the build)

Notes:
- Entries are produced by scanning at run time; the 40 paths are never hard-coded, so adding, renaming or deleting a prefab is a data change and the menu just needs a rerun. The id rule stays single-sourced in ContentNaming.UIPrefabId.
- A missing folder is not an error by itself (the prefab migration may still be in flight): CollectDir logs a warning naming the folder and skips it, and the runner reports the readable Error and aborts only when the collected entry list ends up empty (no empty bundle, no config change).
- Only direct children are collected: the id comes from the file name alone, so a prefab of the same name in a sub-folder would collide.
- AssetDatabase.FindAssets is used rather than raw file enumeration, so an entry is only produced for a prefab Unity has actually imported; the result is sorted because FindAssets order is not guaranteed. Caveat: a *.prefab file that Unity has not imported yet (no .meta) is silently left out of the bundle instead of failing validation — if that ever matters, switch the scan to a file-system enumeration and let the runner's validation report it by path.
- MenuItem needs a compile-time literal, so the menu entry lives here and must be kept in sync with MenuPath by hand; the automation / MCP path is GlobalBundleRunner.Run(new UIPrefabGlobalProvider(), build, interactive: false), which shows no dialog.
- kind is Prefab for every entry: the runtime loads panels and fragments with the Prefab generic.
- BuildWith declares FontGlobalProvider because the panels, fragments and their nested base prefabs reference Assets/Fonts/SIMHEI.TTF. Measured: building the two bundles separately gives a 6187 KB panel bundle with the font implicitly COPIED inside, while declaring both in one BuildAssetBundles call gives a 152 KB panel bundle plus a 6033 KB font bundle (the panel only records a dependency), because Unity's "an asset explicitly assigned to a bundle is no longer copied" rule only holds within one call. The runner merges the two into a single call, so the UI menu alone still yields a correct font bundle; the font bundle body lands in its own StreamingAssets/Fonts/ directory and each directory is swept separately.
- The dependency is by bundle name, so an entry declaring a bundle already in the batch is ignored; BuildWith must not be circular.
- This file carries data and the menu entry only; never re-implement guard, validation, config writing, DB sync or building here.
