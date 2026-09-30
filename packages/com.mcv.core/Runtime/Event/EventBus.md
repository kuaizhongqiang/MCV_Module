# Contract: EventBus

Role: type-safe static generic event bus over Action<T>; one independent subscriber list per closed type T, published synchronously to all current subscribers.

Fields:
s_Subscribers:List<Action<T>> (static)  strong-reference subscriber list, guarded by s_Lock
s_Lock:object (static)  guards the list and the snapshot rebuild
s_Snapshot:Action<T>[] (static)  cached dispatch array, avoiding a per-Publish ToArray
s_Revision:int (static)  bumped by Subscribe / Unsubscribe / Clear
s_SnapshotRevision:int (static)  the revision the snapshot was built from

Methods:
Subscribe(handler)  static; adds the handler when not already present, bumps the revision
Unsubscribe(handler)  static; removes the handler when present, bumps the revision
Publish(eventData)  static; rebuild the snapshot when dirty, then invoke each handler in order
Clear()  static; removes all subscribers of T, bumps the revision
SubscriberCount  static int property

Notes:
- Static generic class: EventBus<A> and EventBus<B> are completely separate lists, so crossing types or sharing a list breaks the isolation.
- Subscription is de-duplicated with Contains, so subscribing the same handler twice still unsubscribes completely.
- The list holds strong references: every subscriber must call Unsubscribe in OnDestroy, otherwise the handler leaks and is later invoked on a destroyed object.
- Publish iterates a stable snapshot array, so subscribing or unsubscribing inside a handler neither throws nor affects the current dispatch; the change applies from the next Publish.
- Dispatch order equals subscription order inside the snapshot and is part of the observable behaviour.
- Each handler is invoked inside try/catch: a throwing subscriber is logged and does not stop the others.
- Clear() wipes every subscriber of that T (used on scene switch); code that must survive a scene switch has to re-subscribe afterwards.
- All list mutations are lock-guarded: the snapshot is copied under the lock and handlers run outside it.
