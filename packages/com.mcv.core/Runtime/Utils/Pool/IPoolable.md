# Contract: IPoolable

Role: callback interface for pooled instances; each instance resets itself on spawn and cleans itself up on despawn, and may implement it on the root or any child node.

Methods:
OnSpawn()  called after the instance is reparented to the target parent and activated
OnDespawn()  called while the instance is still active, before it is reparented to the idle container and deactivated

Notes:
- GameObjectPool scans implementers exactly once at instance creation time and caches them on PooledObject.Callbacks, so components added at runtime are never notified.
- Typical OnSpawn work: reset the local transform, clear timers, restore initial visibility, re-subscribe events.
- Typical OnDespawn work: stop coroutines and animations started by this instance, clear temporary data and unsubscribe events; forgetting to unsubscribe stacks duplicate callbacks on the next reuse.
- OnSpawn runs after activation, so work that must happen once per activation belongs there rather than in OnEnable.
