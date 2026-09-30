# Contract: SingletonBase

Role: non-generic abstract base providing only the framework lifecycle hooks (the isInit flag and the abstract DelayInit coroutine); it holds no instance management and no DontDestroyOnLoad.

Fields:
isInit:bool (protected)  set by the subclass once delayed init has finished

Methods:
IsInit  bool property, get only
DelayInit()  protected abstract coroutine, each subclass implements its own initialisation
Start()  protected virtual; triggers DelayInit once, guarded by isInit

Notes:
- This class owns lifecycle only: instance management and DontDestroyOnLoad deliberately live in SingletonGlobalMgr<T> and must not be lifted here.
- Start runs DelayInit only while isInit is false, so a subclass that sets isInit too early skips its own initialisation.
- The subclass is responsible for setting isInit when initialisation completes; the base never sets it.
- Start is virtual, but overriding it moves the init trigger point and is normally unnecessary.
