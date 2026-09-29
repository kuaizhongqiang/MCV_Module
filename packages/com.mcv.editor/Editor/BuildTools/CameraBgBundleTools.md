# Contract: CameraBgBundleTools

Role: the menu entry for the CameraBg global bundle (bundle CameraBg/camerabg); the pipeline itself lives in Assets/Editor/Common/GlobalBundleRunner and the data lives in BuildTools/GlobalProviders/CameraBgGlobalProvider.cs.

Fields:
Provider:static readonly IGlobalBundleProvider  the CameraBgGlobalProvider singleton this tool drives

Methods:
Build()  [MenuItem("MCV Build/CameraBg 背景图 AB（两张打成一包）", priority 63)] an interactive run
Run(build, interactive)  non-interactive core; delegates straight to GlobalBundleRunner.Run(Provider, build, interactive), and interactive=false shows no dialog at all

Notes:
- The eight pipeline segments (compile guard, asset validation, config writing, read-back self-check, PackageDB sync, dialogs, building, log text) used to live here and were almost line-by-line identical to RoomOneBundleTools; they all moved into GlobalBundleRunner in B1.5, so this file is only a menu plus a provider reference.
- Behaviour is unchanged: same menu text and priority, same config directory, same ids, same bundle name and output directory, same log tag [CameraBgAB] and dialog title, same sweepStale build.
- The entry table (camerabg_room / camerabg_contactor, index order = material slots _Texture_1 / _Texture_2) is preserved verbatim in CameraBgGlobalProvider.
- The index order of the two sprites is the material slot order, so changing the order swaps the two backgrounds; both textures must be imported as Sprite.
- The runtime chain is fixed: CameraBg start, GlobalAssetsMgr.LoadSpritesByPackageIdsAsync, GlobalAddressableMgr reading StreamingAssets/CameraBg/camerabg, Sprite.texture, then the material's _Texture_1 and _Texture_2.
- Run(build, interactive) is additive: the old file only exposed the interactive Build().
