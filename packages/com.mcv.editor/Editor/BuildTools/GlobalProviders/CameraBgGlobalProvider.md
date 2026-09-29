# Contract: CameraBgGlobalProvider

Role: IGlobalBundleProvider for the CameraBg background sprites: two fixed images packed into one global bundle (bundle CameraBg/camerabg); the whole flow lives in Assets/Editor/Common/GlobalBundleRunner.

Fields:
BundleFileName:const string  "camerabg": the bundle file-name segment, must stay lower case
Ids:string[]  the two package config ids (camerabg_room, camerabg_contactor)
AssetPaths:string[]  the two sprite project paths, index aligned with Ids

Methods:
Name  "CameraBg", the provider source name (log tag [CameraBgAB], dialog title "CameraBg AB")
MenuPath  "MCV Build/CameraBg 背景图 AB（两张打成一包）", the menu literal owned by CameraBgBundleTools
ConfigDir  EditorPaths.CameraBgConfigDir
BundleName  EditorPaths.CameraBgBundleDirName + "/" + BundleFileName, i.e. the StreamingAssets-relative path
CleanStaleConfigs  false: this table is fixed at two entries and the folder was never swept (RoomOne / UI sweep)
AbortOnFirstAssetProblem  true: validation stops at the first problem and reports only it (RoomOne / UI collect all)
BuildWith  empty: the two backgrounds only reference in-project sprites, so nothing has to be built in the same BuildAssetBundles call
Collect()  emit the two entries with kind Sprite, index order preserved

Notes:
- The index order of Ids and AssetPaths is the material slot order: index 0 maps to _Texture_1 (base) and index 1 to _Texture_2 (the cross-fade target), so changing the order swaps the two backgrounds.
- The bundle file-name segment must be lower case because the runtime forces the last segment to lower case, which would otherwise 404 on WebGL and Linux.
- Both textures must be imported as Sprite, because the runtime loads them as Sprite and then reads .texture.
- assetKind is written explicitly as Sprite: relying on the SO field default disagrees with the provider-side default.
- The bundle directory belongs to this pipeline alone (independent of Content/), so the content pipeline's sweepStale can never delete it and vice versa.
- BuildWith is empty because the two textures reference no other global bundle, so a separate build copies nobody's assets; CameraBg may be built on its own.
- This file carries data only; never re-implement guard, validation, config writing, DB sync or building here.
