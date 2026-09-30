# Contract: RoomDynamicMgrBase<T>

Role: generic singleton base for the room-dynamic managers; the first Awake claims the slot, later ones destroy themselves.

Fields:
_instance:T  static instance
isInit:bool  protected; flipped at the end of DelayInit
IsInit:bool  read-only view of isInit

Methods:
Instance  static get/set; the getter falls back to FindObjectOfType<T>()
Exists  static; _instance != null
Awake()  virtual; claim _instance + StartCoroutine(DelayInit) when free, else Destroy(this)
DelayInit()  virtual coroutine; yield one frame -> isInit = true

Notes:
- Subclasses override DelayInit for their own init and must keep the isInit flip, or Setup's start chain times out.
- Not the same base as SingletonGlobalMgr: this one finds itself with FindObjectOfType and is not marked global.
