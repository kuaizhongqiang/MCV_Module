# Contract: ABPackageConfigSO

Role: AssetBundle package config; declares the owning clip, the asset kind, the bundle name and the in-bundle asset path, and exposes "bundleName:assetPath" as its load key.

Fields:
clipId:string  owning ProjectClip id (e.g. clip_contactor); the content pipeline aggregates one bundle per clip, empty for non-content assets
assetKind:ContentAssetKind  which generic to load with (default Sprite); must match the imported main asset
bundleName:string  AssetBundle name = path under StreamingAssets without extension; the file-name segment must be lower-case
variant:string  optional AB variant tag (e.g. hd / sd)
assetPath:string  full project path of the asset (e.g. Assets/Art/BG/main_menu_bg.jpg)

Methods:
GetLoadKey()  -> "{bundleName}:{assetPath}" (before the colon locates the package, after it locates the asset)
AutoAssignPath()  editor helper: fill assetPath from sourceAsset when it is empty

Notes:
- Must stay in its own file named after the class (Unity MonoScript rule, see AAPackageConfigSO).
- The bundle file-name segment must be lower-case: GlobalAddressableMgr forces the last segment to lower-case at runtime while the tool writes it verbatim, so a mismatch is invisible on Windows but always 404 on WebGL / Linux.
- assetKind must be accurate: a .png imported as Sprite has Texture2D as its main asset and Sprite as a sub-asset, so loading with the Object generic only yields Texture2D and "as Sprite" stays null.
- The pipeline writes only the assetPath / bundleName strings; sourceAsset stays empty to avoid duplicating the art into resources.assets.
