# Contract: PooledObject

Role: ownership marker automatically attached by GameObjectPool to the root of each pooled instance; it traces any GameObject back to the pool and key that own it.

Fields:
Owner:GameObjectPool (get)  owning pool, cleared when the instance is destroyed
Key:string (get)  owning pool key, usually the bundle config id
Callbacks:IPoolable[]  spawn and despawn callbacks cached at creation, framework internal

Methods:
Bind(owner, key)  internal; sets Owner and Key
BelongsTo(pool)  true when Owner == pool
OnDestroy()  clears Owner and Callbacks so a destroyed instance no longer points at its pool

Notes:
- Managed by the pool: never add or remove it by hand, otherwise the Despawn ownership lookups in GameObjectPool and ObjectPoolMgr fail.
- Marked [DisallowMultipleComponent] because one instance must have exactly one owning pool.
- The callbacks cached here are collected at creation time, which is why components added at runtime are never notified.
- OnDestroy nulls Owner and Callbacks, which is how ownership is dropped when the instance is destroyed externally.
