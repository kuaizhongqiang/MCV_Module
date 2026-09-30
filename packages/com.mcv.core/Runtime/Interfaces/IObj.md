# Contract: IObj

Role: contract for pointer-interactable objects; exposes a component getter, an interactable flag and eight Mo* pointer events.

Fields:
IsInteractable:bool  whether the object is interactable (false == treated as not hit)
MoEnter / MoExit / MoClick / MoClickRight / MoClickDouble / MoDown / MoUp  pointer events (no payload)
MoMove  pointer move carrying the delta

Methods:
GetObj<T>()  GetComponent<T> on this object

Notes:
- Implementer: InteractiveBase (abstract MonoBehaviour) under Objects/Interactives; GlobalInteractiveMgr registers it and dispatches events through it.
- IsInteractable only decides whether the implementation self-subscribes the Mo* events in Awake; toggling it at runtime neither adds nor removes subscriptions and does not change visibility.
- GetObj<T>() is a thin GetComponent wrapper: do not add caching or instantiation there.
