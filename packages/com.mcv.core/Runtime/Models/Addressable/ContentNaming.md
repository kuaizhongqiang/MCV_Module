# Contract: ContentNaming

Role: the single source of the content-AB naming rules; builds config ids, bundle names and room-icon ids, and validates the lower-case bundle file name.

Fields:
ClipIdPrefix  const "clip_"
BundleDirName  const "Content" (first directory segment under StreamingAssets)
BundleFilePrefix  const "clip_"
TaskInfo / TaskStructure / TaskInspection  task segments, lower-cased TaskType names (inspection is 「测量」; principle has NO segment — the principle video never goes through the AB pipeline)
ResourceModel  const "model"
RoomOneBundleDirName  const "RoomOne"
RoomOneBundleFileName  const "roomone" (must be lower-case)
RoomIconIdPrefix  const "roomone_"
RoomOneBundleName  const "RoomOne/roomone"
UIBundleDirName  const "UI"
UIBundleFileName  const "ui" (must be lower-case)
UIPrefabIdPrefix  const "ui_"
UIBundleName  const "UI/ui"
FontBundleDirName  const "Fonts"
FontBundleFileName  const "font" (must be lower-case)
FontAssetIdPrefix  const "font_"
FontSlotLegacy  const "legacy" (the UnityEngine.Font slot)
FontSlotTmp  const "tmp" (the TMPro.TMP_FontAsset slot)
FontBundleName  const "Fonts/font"

Methods:
RoomIconId(clipId)  "roomone_{device}" derived from clip.id (one rule shared by runtime and editor)
RoomIconIdOfDevice(device)  prefix + device
UIPrefabId(prefabName)  "ui_{prefabName}" (panel / fragment prefab name -> package-config id)
FontAssetId(fontId, slot)  "font_{fontId}_{slot}" (one config per shape: font_ui_legacy / font_ui_tmp)
DeviceOf(clipId)  strip the "clip_" prefix, case-insensitive
BundleNameFor(device)  "Content/" + ("clip_" + device).ToLowerInvariant()
ConfigId(device, task, resource)  "{device}_{task}_{resource}"
InfoSpriteId(device, index)  ConfigId(device, "info", index as two digits)
IsBundleFileNameLowerCase(bundleName)  whether the file-name segment (after the last '/') is all lower-case

Notes:
- The naming rule exists only here so the editor pipeline, the runtime and the edit-mode tests cannot drift apart.
- The config id uses camelCase device while the bundle name uses all-lower-case device: two different strings, do not mix them.
- The bundle file-name segment must be lower-case because GlobalAddressableMgr.GetBundleUrl forces the last segment to lower-case at runtime while the tool writes it verbatim; a mismatch is invisible on Windows but always 404 on WebGL / Linux.
- The room-icon package is a single roaming-shared package rather than one-clip-one-bundle: its clipId stays empty, so GetConfigsByClip returns an empty table and the per-clip load/unload chain never carries it.
- UI (UI/ui) is the third global package after CameraBg/camerabg and RoomOne/roomone: same shape, its clipId stays empty so the per-clip load/unload chain never carries it either, and its bundle file-name segment must be lower-case.
- Fonts (Fonts/font) is the fourth global package after those three, same shape again: clipId stays empty so it is resident and the per-clip load/unload chain never takes it away, and its bundle file-name segment must be lower-case.
- One fontId yields two config ids (font_{fontId}_legacy and font_{fontId}_tmp) rather than one config with two fields: the two shapes are two separate assets loaded with two different generics, so the slot belongs in the id.
- DeviceOf uses global::System.StringComparison: the namespace lives under MCV_Module.Models, so a bare System would resolve to MCV_Module.Models.System (CS0234).
