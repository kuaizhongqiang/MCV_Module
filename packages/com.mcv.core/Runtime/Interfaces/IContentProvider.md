# Contract: IContentProvider (+ ContentResourceEntry)

Role: the single extension point for "which assets a ProjectClip must bundle"; collects content entries for the content-AB pipeline.

Fields:
ContentResourceEntry.id:string  bundle-config id; must equal the key actually written in ProjectData.json
ContentResourceEntry.assetPath:string  project asset path; empty means JSON requires it but the asset is missing
ContentResourceEntry.isError:bool  Error level: blocks the build (missing asset, bad id name, duplicate id)
ContentResourceEntry.isExtra:bool  Warning level: reconcile report only (asset exists but JSON does not reference it)
ContentResourceEntry.kind:ContentAssetKind  asset kind; selects the runtime generic and must be exact
ContentResourceEntry.note:string  free-form note for the reconcile report
Name:string  source name in the reconcile report (e.g. InfoSprite)

Methods:
Collect(ProjectClip clip, string device)  collect this clip's resource entries; JSON is the only source -> emits entries even when the asset is missing

Notes:
- Implementers live in the Editor assembly (Assets/Editor/BuildTools/ContentProviders: InfoSpriteProvider, ModelPrefabProvider) and must be registered in the Provider list of Assets/Editor/BuildTools/ContentBundleTools.cs, otherwise the content is never bundled.
- This is an editor-only contract placed in the runtime assembly only to sit beside IController / IElement; the runtime never consumes it.
- JSON is the single source of truth: emitting only existing assets would make the reconcile step miss missing ones, so a null or empty assetPath entry is emitted instead.
- kind must be accurate: atlases are Sprite sub-assets and reading them as Object returns Texture2D.
- id must match the ProjectData.json key verbatim; clipId / device / bundleName are filled by the main flow and providers must not craft them.
