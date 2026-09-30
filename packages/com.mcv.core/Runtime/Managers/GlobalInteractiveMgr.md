# Contract: GlobalInteractiveMgr

Role: per-frame raycast hub for interactive objects; dispatches Mo* straight to the hit object and publishes pooled GlobalInteractionEventData for global subscribers.

Fields:
doubleClickThreshold:float  [SerializeField] double-click window, 0.2
rayMaxDistance:float  [SerializeField] ray length, 300
objDict:Dictionary<InteractiveBase, bool>  registered interactive objects
currentObj:InteractiveBase  object under the cursor
Current:InteractiveBase  public read-only view of currentObj
ray / raycast / cam / mouse  per-frame temporaries
lastClickTime:float  last single click time, -1 until the first click
m_MouseIdle:bool  synced from MouseMoveStateEventData

Methods:
Awake()  base + cache Mouse.current + subscribe MouseMoveStateEventData
OnDestroy()  unsubscribe
Update()  gated by isInit, by idle-and-no-button, and by UI hover -> CoreDetect()
DelayInit()  wait until GlobalCameraMgr.Camera is non-null -> isInit
OnMouseMoveStateChanged(e)  store IsIdle
Register(interactive) / Unregister(interactive)  static; keep objDict in sync
CollectRegistered<T>(results)  static; fill the caller's list with registered T, no GC, uses SafeInstance
CoreDetect()  raycast -> Enter/Exit/Down/Up/Click/ClickDouble/ClickRight/Move on the object plus matching global events; on miss -> Exit, and on left release a Click with a null target
PublishInteraction(target, type, delta)  pooled event, published synchronously and released right after
ifUiBlockRayCast()  true when the pointer is over UI (skips scene interaction)

Notes:
- Events go to the hit object directly (O(1) instead of broadcasting to every interactive); the global event exists so the line state machine and step conditions can subscribe.
- Idle gating: while the mouse is idle and no button state changed this frame the raycast is skipped, so hover moves caused by objects moving under a still cursor surface on the next input (accepted for teaching scenes).
- A null-target Click is the "clicked empty space" signal, used e.g. to cancel a line in progress.
- Registration is what makes CollectRegistered possible; it replaces FindObjectsOfType-style scene scans.
