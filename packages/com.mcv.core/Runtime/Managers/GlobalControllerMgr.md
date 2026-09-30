# Contract: GlobalControllerMgr

Role: creates, registers, looks up and destroys all IController instances, and hosts every controller coroutine.

Fields:
s_ControllerTypes:HashSet<Type>  static type table; filled by each controller's static constructor and by a reflection sweep
_controllers:Dictionary<string, IController>  name -> controller
_byType:Dictionary<Type, IController>  concrete type -> controller
_routines:Dictionary<IController, List<Coroutine>>  per-controller coroutine handles

Methods:
RegisterControllerType(type)  static; add a controller type (called by ControllerBase<TView>'s static constructor)
DelayInit()  CreateAll() then isInit = true (logs the elapsed ms when slow)
CreateAll()  create + register every type in the table (idempotent)
Create(type)  create one controller, register by type and name, then call OnInit()
Unregister(controller)  stop its coroutines -> OnDispose -> drop from both tables
DisposeAll()  stop + dispose every controller (idempotent; called on quit)
Find<T>()  lookup by concrete type
Find(name)  lookup by name; null when absent
Exist<T>()  type-table presence
All  IReadOnlyCollection<IController> of registered controllers
RunCoroutine(owner, routine)  start a coroutine on this manager, tracked under owner
StopCoroutine(owner, routine)  stop one tracked coroutine
StopAllCoroutines(owner)  stop every coroutine of one controller

Notes:
- Controllers are NOT MonoBehaviours and are NOT in any scene: the 27 former ControllerRoot children were deleted. This manager is the only creation point.
- Creation happens inside DelayInit, and Setup waits for IsInit before loading 1_Content, which is what guarantees "registered earlier than the first panel Bind".
- The controller is persistent for the app lifetime (the manager is DontDestroyOnLoad), matching the old ControllerRoot semantics.
- Coroutines run here rather than on a controller object, so they must be stopped explicitly: OnDispose/DisposeAll stop them per controller.
- Controller types are indexed by name AND by type; two controllers sharing a simple class name collide in the name index (a warning is logged and the later one wins).
- Missing a type in the table no longer silently drops it: the table getter does a reflection sweep of the assembly when empty.
