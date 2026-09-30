# Contract: PackageDatabaseSO

Role: master list of package configs; holds every PackageConfigSO reference and offers filtering, lookup, integrity checks and editor-side auto-collection.

Fields:
packages:List<PackageConfigSO>  [SerializeField] all managed configs, AA / AB / Default mixed
Count:int  read-only count

Methods:
GetByType(type)  filter by package type
FindById(id)  find a config by id, null when absent
Validate()  list of indices whose entry is null or has an empty id
AutoCollect()  editor only: scan "t:PackageConfigSO" plus every concrete subclass, de-duplicate by asset path, sort by id -> rewrite packages + EditorUtility.SetDirty

Notes:
- Runtime entry point: GlobalAddressableMgr loads this SO and builds the id -> PackageConfigSO map, so a missing entry means a load failure at runtime.
- AutoCollect cannot rely on "t:PackageConfigSO" alone: the AssetDatabase type filter does not match subclasses of an abstract base (48 on-disk ABPackageConfigSO with an empty packages list was observed), hence the base plus each concrete subclass are searched and merged.
