# Contract: PackageConfigSO

Role: abstract base of every package config; one .asset per packageable resource, shared by the editor build tools and the runtime loader.

Fields:
id:string  [SerializeField] unique id used at runtime to find and load the asset; must not collide with other packages
displayName:string  [SerializeField] display name for the editor and logs, unused at runtime
sourceAsset:Object  [SerializeField] the asset to pack; a direct reference, left empty by the AB / AA pipeline
PackageType:PackageType  abstract read-only, fixed by the subclass
SourceAssetPath:string  editor-only read-only project path derived from sourceAsset

Methods:
GetLoadKey()  abstract runtime load key: AA -> address, AB -> bundleName:assetPath, Default -> id

Notes:
- The base and its three subclasses must live in four separate files named after their classes: Unity only generates a MonoScript for a class whose name equals the file name, otherwise CreateInstance + CreateAsset yields m_Script = 0 (lost script, loads as null, which shows up as "the config was created but the database never sees it").
- sourceAsset is a direct reference: when the config asset sits under Resources/, the referenced art is also packed into resources.assets, duplicating it and defeating the AB pipeline, so the AB pipeline leaves it empty.
