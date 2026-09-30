# Contract: PackageType

Role: enumerates the asset load strategies; each value maps to one full load chain routed by GlobalAddressableMgr.

Fields:
Default  direct local reference (Resources / serialised reference), bypasses the package system
AA  Addressables system (hot update, dependency management, reference counting)
AB  classic AssetBundle loaded from StreamingAssets, manual lifetime

Methods:
(none)

Notes:
- The enum value is the load strategy: GlobalAddressableMgr routes AA / AB / Default by it, so changing the values steers which chain a resource takes.
- The runtime registry was removed; the id -> PackageConfigSO map is built by GlobalAddressableMgr.BuildConfigMap.
