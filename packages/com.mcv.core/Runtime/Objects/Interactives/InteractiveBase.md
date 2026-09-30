# Contract: InteractiveBase

Role: base of every clickable object; self-registers with GlobalInteractiveMgr and turns pointer callbacks into Mo* events. Hover presentation is not part of the framework: a host that wants highlight subscribes to MoEnter / MoExit.

Fields:
isInteractable:bool  [SerializeField] default true; gates both the Mo* self-subscription in Awake and runtime hit-testing
MoEnter / MoExit / MoClick / MoClickRight / MoClickDouble / MoDown / MoUp:event Action  pointer events (MoMove is Action<Vector2>)
IsInteractable  get/set gate; false == "not hit" for the manager

Methods:
Awake()  when isInteractable, subscribe the Mo* events to the empty virtual hooks -> GlobalInteractiveMgr.Register(this)
OnDestroy()  when isInteractable, unsubscribe -> GlobalInteractiveMgr.Unregister(this)
GetObj<T>()  GetComponent<T>()
Mo*Event()  empty virtual hooks; subclasses override
InvokeMoEnter / InvokeMoExit / InvokeMoClick / InvokeMoMove(Vector2) ...  re-raise the matching event; called by GlobalInteractiveMgr

Notes:
- Toggling IsInteractable at runtime never adds or removes the Mo* subscriptions: that decision is made once, in Awake.
- There is no highlight service and no highlight colour here: the framework defines no plug-in abstraction, and the MoEnter / MoExit events are the only hook a host gets.
- Subclasses overriding Awake/OnDestroy must call base: registration and event wiring live there.
- This is the script that makes an object raycastable by GlobalInteractiveMgr; the component must sit on the collider's own GameObject.
