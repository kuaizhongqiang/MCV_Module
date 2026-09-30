# Contract: SingletonGlobalMgr

Role: generic global-manager singleton base; thread-safe global uniqueness, DontDestroyOnLoad persistence across scenes and delayed init through SingletonBase.DelayInit.

Fields:
s_Instance:T (static)  the single instance, per closed type T
s_Lock:object (static)  guards lazy creation
s_AppQuitting:bool (static)  suppresses instance access and creation during shutdown

Methods:
Instance  static T property; returns the unique instance, lazily finding or creating it
Exists  static bool property; s_Instance != null
SafeInstance  static T property; null while quitting, never triggers creation
Awake()  protected virtual; registers the instance, keeps only one and sets DontDestroyOnLoad
OnApplicationQuit()  protected virtual; sets s_AppQuitting
OnDestroy()  protected virtual; sets s_AppQuitting when this is the current instance

Notes:
- Each closed type T holds its own static instance: never lift s_Instance or Awake into a non-generic base, or different managers would share one instance.
- Instance looks the instance up with FindObjectsByType<T>, using the concrete type T so subclasses are not confused with each other.
- When no instance exists, Instance creates a GameObject named "{T} (Singleton)", adds component T and marks it DontDestroyOnLoad; the creation happens inside s_Lock.
- Awake keeps a single instance: the first survives and gets DontDestroyOnLoad, later duplicates are logged and destroyed. Do not remove that guard.
- s_AppQuitting makes Instance log a warning and return null instead of resurrecting an object during shutdown, so never create new singletons while quitting.
- Do not cache the instance across shutdown-prone scene transitions; use Exists or SafeInstance when unsure.
