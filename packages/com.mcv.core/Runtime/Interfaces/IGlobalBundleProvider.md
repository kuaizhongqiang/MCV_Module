# Contract: IGlobalBundleProvider (+ GlobalResourceEntry)

Role: the single extension point for "which assets a global bundle must carry"; a global bundle is one bundle of shared assets with no ProjectClip (`clipId` empty), e.g. CameraBg / RoomOne / UI / Fonts.

Fields:
GlobalResourceEntry.id:string  bundle-config id (= the runtime lookup key)
GlobalResourceEntry.assetPath:string  project asset path; empty means the entry points at a missing asset
GlobalResourceEntry.kind:ContentAssetKind  asset kind; selects the runtime generic and must be exact
GlobalResourceEntry.note:string  free-form note for the reconcile report
Name:string  source name in the reconcile report and the log tag (e.g. UI)
MenuPath:string  tool menu item (e.g. MCV Build/UI prefab AB)
ConfigDir:string  package-config folder (e.g. Assets/Resources/Config/UIPackages)
BundleName:string  bundle name (e.g. UI/ui); the output folder is Assets/StreamingAssets/<first segment>
Collect():IEnumerable<GlobalResourceEntry>  collect every entry of this bundle
BuildWith:IEnumerable<IGlobalBundleProvider>  the other global bundles that must be built inside the SAME BuildAssetBundles call as this one (empty when this bundle's assets reference no other global bundle)
CleanStaleConfigs:bool  true = sweep configs under ConfigDir whose id is not part of this run (RoomOne / UI); false = leave the folder untouched (CameraBg, whose fixed two-entry table never swept it)
AbortOnFirstAssetProblem:bool  true = stop at the first validation problem and report only that one (CameraBg); false = collect every problem, log them all and show the first six (RoomOne / UI)

Methods:
GlobalResourceEntry() / GlobalResourceEntry(id, assetPath, note, kind)  the kind defaults to Prefab

Notes:
- Implementations live in the Editor assembly (Assets/Editor/BuildTools/GlobalProviders: CameraBgGlobalProvider / RoomOneGlobalProvider / UIPrefabGlobalProvider / FontGlobalProvider) and are driven by Assets/Editor/Common/GlobalBundleRunner.cs; the runtime never consumes this contract.
- The two policy flags plus BuildWith are the only per-bundle differences besides the entry table; the flags exist so the runner needs no per-provider branch to preserve CameraBg's legacy "no config sweep, abort on the first bad asset" behaviour.
- GlobalResourceEntry.note is appended in parentheses to the asset-validation problem message (RoomOne uses it to keep the "file name must match character for character" hint), so it must stay a short parenthetical hint.
- A global bundle must keep `clipId` empty: GlobalAddressableMgr.GetConfigsByClip returns an empty list for an empty clipId, and UnloadClip filters by clipId, so a global bundle is resident by construction and never swept away by the per-clip load/unload path.
- The bundle name's file-name segment must be lower-case: GlobalAddressableMgr.GetBundleUrl lowercases it, and a mismatch 404s on WebGL / Linux.
- BuildWith exists because Unity's "an asset explicitly assigned to a bundle is no longer copied" rule only holds WITHIN one BuildAssetBundles call. Measured on TipsPanel.prefab (which nests a base prefab that references SIMHEI.TTF directly): declaring only the panel bundle gives a 6187 KB panel bundle with the font implicitly COPIED into it, while declaring the panel bundle and the font bundle in the same call gives a 152 KB panel bundle plus a 6033 KB font bundle (the panel only records a DEPENDENCY). The runner builds one bundle per provider, so building the font bundle first and the UI bundle second would still copy the font into the UI bundle and "one copy of each font shape, inside the font bundle" could never hold. UIPrefabGlobalProvider therefore declares FontGlobalProvider here; the runner merges this provider plus its BuildWith providers into one AssetBundleBuild[] and calls BuildPipeline.BuildAssetBundles exactly once.
- Each provider's menu still runs only itself (Run(thisProvider, ...)); the runner pulls the dependent bundles into the same build through BuildWith, so all four menus produce correct output and nobody has to know the ordering rule.
- BuildWith may not be circular and is deduplicated by bundle name; a bundle declared both as the main provider and as a dependency is built once.
- The output folder must be its own segment (Assets/StreamingAssets/UI/): BundleBuilder's sweepStale deletes anything not produced by the current run, so sharing a segment with Content/ makes the two pipelines delete each other's output.
- kind must be accurate: atlases are Sprite sub-assets and reading them as Object returns Texture2D.
- A global bundle cannot go through IContentProvider: that one collects per ProjectClip.
