# Contract: ElementObjBase (+ ElementNameMap)

Role: base of every element (holds DataBase, waits for the parent ElementManagerBase, then names and registers itself); ElementNameMap maps ElementType / ElementPointNameType to display symbols and back.

Fields:
data:DataBase  [SerializeField] element data (id / displayName ...); exposed read-only as Data
isInit:bool  runtime; true once DelayInit finished naming and registering
Type:ElementType  abstract virtual; each subclass returns its own kind
cachedParent:Transform  runtime only; cached transform.parent
cachedManager:ElementManagerBase  runtime only; resolved from cachedParent
ElementNameMap.ElementRemap / PointRemap  static readonly dictionaries (enum -> symbol)
ElementNameMap.ElementReverse / PointReverse  static readonly dictionaries (symbol -> enum)

Methods:
Awake()  base -> StartCoroutine(DelayInit())
DelayInit()  poll GetManagerCached until non-null -> name = GetName(), data.id = name -> mgr.RegisterElement(this) -> isInit = true
OnDestroy()  base -> ElementManagerBase.Instance.UnregisterElement(this)
GetManagerCached()  re-resolve the parent manager only when transform.parent changed
GetName()  ElementNameMap.GetName(Type) + data.displayName
Mo*Event()  empty overrides; subclasses own the behaviour
ElementNameMap.GetName(ElementType) / GetName(ElementPointNameType)  symbol lookup; "None" when the key is missing
ElementNameMap.GetElementType(string) / GetPointNameType(string)  reverse lookup; None when the key is missing

Notes:
- An element must sit under an ElementManagerBase, otherwise DelayInit never completes (no name, no registration).
- Naming writes both gameObject.name and data.id; re-parenting or reordering changes the ids that conditions and line matching rely on.
- Point symbols are the wiring semantics (1L1 / 13NO / A1 / PE ...); editing PointRemap breaks line-completion checks.
- Duplicate symbols keep the first entry (Fuse and Resistor are both "R"); the reverse tables are flipped from the forward ones in the static constructor.
