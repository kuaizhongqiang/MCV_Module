# Contract: AAPackageConfigSO

Role: Addressable Assets package config; declares the runtime address, the batch-loading labels and the AA group name, and exposes the address as its load key.

Fields:
address:string  the Addressables address, i.e. the runtime load key
labels:string[]  labels for batch loading and group filtering
groupName:string  AA group name, editor only

Methods:
GetLoadKey()  -> address
AutoAssignAddress()  editor helper: fill the address from sourceAsset's file name when it is empty

Notes:
- Must stay in its own file named after the class: Unity only generates a MonoScript for a class whose name equals the file name, so a derived SO sharing the base-class file yields an asset with m_Script = 0 (lost script, loads as null).
- The runtime loads by address only; sourceAsset stays empty so the referenced art is not duplicated into resources.assets.
- AutoAssignAddress must only fill an empty address, otherwise it overwrites a hand-written one.
