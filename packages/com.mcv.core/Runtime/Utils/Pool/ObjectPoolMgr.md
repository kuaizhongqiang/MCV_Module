# Contract: ObjectPoolMgr

Role: pool registry managing several GameObjectPool instances keyed by an id (usually the bundle config id); hierarchy is Root -> one container per key -> that key's idle instances.

Fields:
m_Pools:Dictionary<string, GameObjectPool>  key to pool
m_Root:Transform  pool container root
m_OwnsRoot:bool  true when the registry created its own root
Root:Transform / PoolCount:int / Keys  root, pool count and key enumeration

Methods:
ctor(root)  self-builds an "__ObjectPools" root when root is null
Get(key) / Contains(key)  look the pool up
CreatePool(key, prefab, maxIdleCount, preload, instantiate)  create a pool, never overwriting an existing key, optionally preloading
DestroyPool(key, destroyInstances) / DestroyAllPools(destroyInstances) / Dispose(destroyInstances)  teardown paths
Spawn(key, parent) / Spawn(key, parent, localPosition, localRotation)  spawn through the registered pool
Despawn(instance) / DespawnAll(key) / DespawnAll()  return instances through the owning pool
ActiveCount(key) / IdleCount(key) / ActiveCount()  counters, zero when the pool is missing

Notes:
- Not a global manager: it is a plain utility class, deliberately not a SingletonGlobalMgr and not part of the Setup chain. Whoever needs pools holds its own registry (InstManagerBase per manager, GlobalAssetsMgr one shared global instance).
- CreatePool returns an existing pool untouched: it neither overwrites nor re-runs preload, so live instances are never lost. A null key or prefab returns null.
- The per-key container node is created and owned by this registry, so DestroyPool and Dispose destroy it; the pool only destroys a root it created itself.
- Dispose destroys m_Root only when the registry built it; a caller-provided root survives.
- Spawn logs an error and returns null when the key has no pool, so the caller must CreatePool first or go through GlobalAssetsMgr / the InstManager spawn path.
- Despawn rejects instances without a PooledObject marker or without an Owner, guarding against returning an object to the wrong pool.
