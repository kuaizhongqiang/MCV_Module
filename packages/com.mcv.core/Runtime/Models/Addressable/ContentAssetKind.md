# Contract: ContentAssetKind

Role: enumerates the resource kinds; selects which generic the runtime loads with, and which validation branch the editor pipeline runs.

Fields:
Sprite  = 0, texture, loaded with the Sprite generic (sub-asset)
Prefab  = 1, prefab, loaded with the GameObject generic
Font  = 2, legacy UnityEngine.Font, loaded with the Font generic (font global bundle only)
TmpFont  = 3, TMPro.TMP_FontAsset, loaded with the TMP_FontAsset generic (font global bundle only)

Methods:
(none)

Notes:
- Must be recorded explicitly because Unity filters by the generic parameter: a .png imported as Sprite has Texture2D as its main asset, so LoadAssetAsync<Object> only yields Texture2D and "as Sprite" is always null.
- Sprite sets must record Sprite and models must record Prefab, or the load silently comes back empty.
- Produced by the Provider, written by ContentBundleTools into ABPackageConfigSO.assetKind and read by GlobalAssetsMgr to pick the generic.
- assetKind is NOT a dummy value for packages that ignore it: GlobalBundleRunner.Validate branches on kind (Sprite to Texture2D + TextureImporter, Font / TmpFont to a main-asset null check, everything else to GameObject) and GlobalAssetsMgr.PreloadGlobalBundleRoutine splits its generic on assetKind as well.
- Font / TmpFont serve the font global bundle only (B3). The per-clip content providers never emit them, so the content load/unload path (LoadClipRoutine's "Sprite / otherwise GameObject" two-way branch) is unchanged.
- Values may only be appended, never renumbered: assetKind is serialized as an integer inside existing ABPackageConfigSO assets, so changing an existing value would silently reinterpret old configs.
