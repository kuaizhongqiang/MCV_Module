# Contract: InstManagerBase

Role: instance-manager base: load a prefab by package-config id, then take from / return to a per-package object pool; subclassed by InstShowManager and InspectionManager.

Fields:
mainCamController:InputControllerBase  [SerializeField] camera controller toggled together with control mode
packageKeys:string[]  [SerializeField] package-config ids, one prefab each
objParent:Transform  [SerializeField] protected; parent of active instances; empty falls back to InstRoot in 1_Content
preloadCount:int  [SerializeField] instances pre-created per package, 0 = load only
maxIdleCount:int  [SerializeField] idle cap per package, excess destroyed on return; <=0 = unlimited, 24
maxWaitTime:float  [SerializeField] longest wait for GlobalAddressableMgr / prefab callbacks (s), 5
movePoolToInstScene:bool  [SerializeField] move the idle container under 1_Content so it dies with that scene
isControlled:bool  protected; set by the subclass (display-only = false)
AllowEmptyPackageKeys:bool  protected virtual; true = on-demand type, empty keys are legal and nothing is preloaded
isAllPackagesLoaded:bool  protected; precondition of a synchronous Spawn
prefabCache:Dictionary<string, GameObject>  package id -> loaded prefab
pool:ObjectPoolMgr  protected pool registry keyed by package id
poolRoot:Transform  idle-instance container
isLoadingPackages:bool  re-entry guard between DelayInit and InitManager
IsReady / PackageCount / Pool  public read-only views
InstSceneName / InstRootName / PoolRootName  const "1_Content" / "InstRoot" / "__InstPool"

Methods:
Awake()  virtual; EnsurePool only, never touches static singletons (subclasses bind theirs first)
DelayInit()  empty keys -> on-demand type returns immediately, otherwise an error after maxWaitTime; else RunLoad + optional SetSceneRoot -> isInit
OnDestroy()  virtual; pool.Dispose(true), then clear the prefab cache
LoadPackageAsync(keys, onComplete)  virtual; wait for GlobalAddressableMgr.IsInit, then load each key and create its pool; a single failure only logs
LoadPrefabRoutine(id, onLoaded)  wrap the callback-based LoadPrefabAsync into a coroutine with a timeout
CreateTrackedInstance(key, prefab, parent)  create through GlobalAddressableMgr.InstantiatePrefab so the package registration exists
RunLoad(keys)  re-entry guard + LoadPackageAsync
InitManager(keys, isControlled) / InitManager(keys)  runtime injection; kicks off a load when needed
SetParent(parent)  move objParent and the existing pool container
Spawn(id, parent) / Spawn(id, parent, pos, rot)  synchronous; null when the package was never preloaded
Spawn<T>(...)  synchronous spawn plus GetComponentInChildren; the instance is returned when the component is missing
SpawnAsync(id, parent, onSpawned, onError)  already pooled -> callback at once; otherwise load, pool, then spawn
Despawn(instance) / DespawnAll() / DespawnAll(packageId)  return to the pool
Preload(packageId, count)  create instances up front
ReleasePackage(packageId, destroyInstances=true) / ReleaseAllPackages(destroyInstances=true)  drop pools and prefab cache
GetSceneRoot()  InstRoot under 1_Content, created when missing; null while that scene is unloaded
SetSceneRoot()  move the pool container under the scene root
ResolveSpawnParent()  objParent, else the pool container
EnsurePool() / EnsurePoolRoot() / CreatePoolFromCache(packageId)
SetPackageKeys / SetControlled  private setters
HasMainRegisted() / SetMainControllerActive(isActive)  toggle the main camera controller

Notes:
- Prefabs always come through GlobalAssetsMgr.LoadPrefabAsync -> GlobalAddressableMgr (AA / AB / Default); this class never calls Resources.Load or AssetBundle.LoadAsset itself.
- Instances created via InstantiatePrefab are registered to their package, so unloading a package destroys them instead of leaving resource-less pink objects.
- Pooled instances reset through IPoolable: OnSpawn on take, OnDespawn on return.
- Inst* managers are not part of Setup's start chain, so LoadPackageAsync waits for GlobalAddressableMgr on its own.
- Spawn is synchronous only because DelayInit preloaded and pooled everything; a package first seen at runtime needs SpawnAsync.
- One pool per package-config id gives zero Instantiate and zero disk IO in the steady state (Utils/Pool).
