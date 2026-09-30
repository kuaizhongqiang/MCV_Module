# Contract: GameObjectPool (+ PoolInstantiateFunc)

Role: object pool for a single prefab; Spawn reuses idle instances first and only instantiates when idle is empty, Despawn fires IPoolable.OnDespawn, reparents to the idle container and deactivates.

Fields:
m_Idle:Stack<GameObject>  idle instances (may hold externally destroyed slots)
m_Active:HashSet<GameObject>  currently spawned instances
m_Instantiate:PoolInstantiateFunc  creation factory; null means Object.Instantiate
m_OwnsRoot:bool  true when the pool created its own idle container
Key:string (get) / Prefab:GameObject (get)  pool key (usually the bundle config id) and the managed prefab
Root:Transform (get) / MaxIdleCount:int (get, set) / IdleCount / ActiveCount / TotalCount:int (get)  container and counters

Methods:
ctor(key, prefab, root, maxIdleCount, instantiate)  throws on a null prefab, self-builds Root when root is null
Spawn(parent) / Spawn(parent, localPosition, localRotation)  reuse or create, reparent, place, activate, notify
Despawn(instance)  validate ownership, OnDespawn, reparent to Root, deactivate, destroy when over the idle cap
DespawnAll() / Contains(instance) / Preload(count)  despawn everything, test ownership, warm up
ClearIdle(trimToZero) / Destroy(destroyActiveInstances)  drop idle slots / destroy the pool
Acquire() / CreateInstance() / DestroyInstance(instance)  internal reuse and creation path
Notify(instance, isSpawn) / IsAlive(callback) / PruneDestroyed() / Sanitize(key)  callback replay and bookkeeping

Notes:
- PoolInstantiateFunc lets the asset pipeline own instantiation, e.g. GlobalAddressableMgr.InstantiatePrefab which registers the instance-to-bundle ownership so unloaded bundles destroy their instances.
- Double despawn or despawn of a non-spawned instance: Despawn checks marker.Owner == this (otherwise logs a warning) and m_Active.Remove, so a repeat call just returns false and is silently ignored.
- Instances destroyed externally are tolerated: Acquire skips null idle slots, PruneDestroyed clears null active slots and IsAlive guards callbacks whose component was destroyed.
- Lifecycle hooks are gathered once at creation (GetComponentsInChildren<IPoolable>(true)) and cached on PooledObject.Callbacks, so components added later are never notified. OnSpawn runs after reparenting and activation, OnDespawn while the instance is still active.
- An active prefab runs Awake / OnEnable on the same frame it is instantiated and is only deactivated afterwards, so business start-up logic must live in OnSpawn, never in OnEnable.
- MaxIdleCount <= 0 means unlimited; when idle is already at the cap, Despawn destroys the returned instance but still reports success. Preload only fills idle and stops early when creation fails.
- CreateInstance deactivates and renames the new instance immediately, so it never flashes inside the pool and loses the "(Clone)" suffix.
- Destroy always clears idle; destroyActiveInstances = false only prunes destroyed active slots. A root owned by the pool (m_OwnsRoot) is destroyed too and Root is nulled.
