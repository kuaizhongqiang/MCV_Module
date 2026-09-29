# Contract: FontGlobalProvider

Role: IGlobalBundleProvider for the font global bundle (B3): every FontCatalogSO entry contributes its legacy TTF and its TMP SDF asset, all of them packed into one bundle (bundle Fonts/font), so the fonts stop living in the default resources.assets; the whole flow lives in Assets/Editor/Common/GlobalBundleRunner.

Methods:
Name  "Font", the provider source name (log tag [FontAB], dialog title "Font AB")
MenuPath  "MCV Build/字体 AB", the menu literal owned by the Build() entry in this file
ConfigDir  EditorPaths.FontConfigDir (Assets/Resources/Config/FontPackages)
BundleName  ContentNaming.FontBundleName (Fonts/font; output directory Assets/StreamingAssets/Fonts/)
CleanStaleConfigs  true: a font removed from the catalog (or a renamed fontId) would otherwise leave a config whose id resolves to nothing
AbortOnFirstAssetProblem  false: one run reports every problem at once (empty path, bundle-name mismatch, duplicate id)
BuildWith  empty: the TTF / SDF assets reference no other global bundle, so running the font menu alone builds only the font bundle, which is correct
Collect()  load the FontCatalogSO and emit two entries per fontId: id = ContentNaming.FontAssetId(id, FontSlotLegacy) with kind = Font for legacyFontPath, and id = ContentNaming.FontAssetId(id, FontSlotTmp) with kind = TmpFont for tmpFontAssetPath; a slot with an empty path is skipped with a warning
AddSlot(list, entry, slot, assetPath, kind)  private: skip + warn on an empty path, otherwise append the entry with an explicit kind
LoadCatalog()  private: AssetDatabase.FindAssets("t:FontCatalogSO") -> GUIDToAssetPath -> LoadAssetAtPath; logs an Error and returns null when there is no catalog or it fails to load, and warns when more than one catalog exists
Build()  static [MenuItem] entry, interactive run (confirm dialog before the build)

Notes:
- The entries come from FontCatalogSO rather than a directory scan or a hard-coded table: adding a font is a data-only change with zero code, which is the point of this provider. The catalog stores path strings only and holds no Font / TMP_FontAsset reference, so the catalog asset itself cannot drag the 33.8MB TMP asset back into resources.assets.
- One fontId produces two entries, not one: the two shapes are two separate assets loaded with two different generics (UnityEngine.Font / TMPro.TMP_FontAsset), so the slot is part of the config id (font_ui_legacy / font_ui_tmp).
- bundleName is strongly validated: every entry's bundleName must equal ContentNaming.FontBundleName, otherwise an Error is logged and an empty list is returned (abort). The catalog's bundleName is exactly what FontCatalog.BundleNameOf(fontId) feeds to the runtime preload, and GlobalAddressableMgr.GetConfigsByBundleName matches by exact string, so a mismatch would show up as "the font bundle is built but nothing is ever found".
- A missing / unloadable FontCatalogSO logs an Error and returns an empty list; the runner then aborts through its own "no entries collected" guard, so no empty bundle is produced and no config is touched.
- An empty id is skipped with a warning because the id is the runtime fetch key (font_{id}_{slot}) and cannot be derived otherwise.
- A slot with an empty path is skipped with a warning rather than failing the run: a missing TMP asset must not keep the legacy font out of the bundle.
- kind is written explicitly for every entry; leaving it to the field default would silently mean Prefab and the runtime would load the font with the GameObject generic.
- More than one FontCatalogSO in the project is a warning: the runtime resolves a fixed literal path (Resources/Config/FontCatalog), so any extra catalog is read by nobody.
- BuildWith is empty because fonts reference no other global bundle: the font menu on its own produces a correct font bundle and nothing is copied into it. The opposite direction is declared by UIPrefabGlobalProvider, which pulls this bundle into its own single BuildAssetBundles call.
- MenuItem needs a compile-time literal, so the menu entry lives here and must be kept in sync with MenuPath by hand; the automation / MCP path is GlobalBundleRunner.Run(new FontGlobalProvider(), build, interactive: false), which shows no dialog.
- This file carries data and the menu entry only; never re-implement guard, validation, config writing, DB sync or building here.
