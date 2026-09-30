# Contract: GlobalAddressableMgr

Role: package-config registry and the single asset load/unload entry for the AA / AB / Default chain; caches assets per package-config id, instantiates prefabs and registers instance-to-package ownership, and loads/unloads AA scenes for GlobalSceneMgr.

Fields:
m_PackageDatabases:List<PackageDatabaseSO>  [SerializeField] package-config databases; empty -> Resources/Config/PackageDB_Master
m_SceneConfig:SceneAddressableConfig  [SerializeField] scene AA table; unset -> Resources/Config/SceneAAConfig
m_ConfigMap:Dictionary<string, PackageConfigSO>  config id -> config (BuildConfigMap)
m_BundleCache:Dictionary<string, AssetBundle>  bundle file name -> loaded bundle (key is the bundle name, not the config id)
m_AssetCache:Dictionary<string, Object>  config id -> loaded asset; one shared cache for all three PackageTypes
m_InstanceMap:Dictionary<GameObject, string>  live instance -> config id; the reason DestroyInstances / UnloadBundle(...,true) can clean up
m_SceneHandles:Dictionary<string, AsyncOperationHandle<SceneInstance>>  sceneName -> handle, written on runtime AA load only
m_SceneMap:Dictionary<string, SceneAAEntry>  scene address -> entry
m_SceneNameToAddress:Dictionary<string, string>  scene name -> address, for O(1) IsSceneAA / GetSceneAddress

Methods:
IsSceneAA(sceneName) / GetSceneAddress(sceneName)  reverse-index lookups; unknown -> false / null
LoadSceneAsync(sceneName, mode)  Editor: EditorSceneManager.LoadSceneAsyncInPlayMode; Runtime: Addressables.LoadSceneAsync -> handle kept in m_SceneHandles; unknown -> error
UnloadSceneAsync(sceneName)  Editor: SceneManager; Runtime: Addressables.UnloadSceneAsync(handle, true) then drop the handle; falls back to SceneManager without a handle
GetConfig(id) / GetConfig<T>(id) / TryGetConfig(id, out config)  config lookup by id; absent -> null / false
GetAllConfigs(type) / ConfigCount / GetClipIds()  lazy enumeration of configs by PackageType / count / distinct non-empty clipIds (diagnostics)
GetConfigsByClip(clipId)  every ABPackageConfigSO whose clipId matches; used by GlobalAssetsMgr for one-clip load and unload
GetConfigsByBundleName(bundleName)  every ABPackageConfigSO whose bundleName matches; the aggregation key for global packages, the same key UnloadByBundleName unloads by
TryGetCached<T>(packageId, out asset)  cache read only, never loads; false for not-loaded / wrong type / destroyed
UnloadClip(clipId)  invalidate that clip's asset cache first, then UnloadBundle(bundleName, true) per distinct bundle
UnloadByBundleName(bundleName)  for global packages with an empty clipId (RoomOne/roomone, CameraBg/camerabg): invalidate by bundle name, then unload; an empty name returns 0 and unloads nothing
LoadAssetAsync<T>(packageId, onLoaded) / LoadAssetAsync<T>(config, onLoaded)  resolve the config then LoadAssetRoutine; unknown -> onLoaded(null)
LoadAssetsAsync<T>(packageIds, onLoaded)  ordered batch; a missing config logs and is skipped
LoadAssetRoutine<T>(config, onLoaded)  cache hit of the same type -> immediate callback; else dispatch AA -> LoadFromAA, AB -> LoadFromAB, Default -> LoadFromDefault
InstanceCount / InstantiatePrefab(prefab, packageId, parent)  prune then count; instantiate, rename to the prefab name (drop "(Clone)"), register instance -> packageId
InstantiateAsync(packageId, parent, onComplete) / InstantiateAsync(config, parent, onComplete)  load the prefab through the same pipeline, then instantiate; failure -> onComplete(null)
ReleaseInstance(instance, destroy)  unregister and optionally destroy; pooled instances must go back to their pool instead
DestroyInstances(packageId) / DestroyAllInstances()  destroy registered live instances, return the count
UnloadBundle(bundleName, unloadAllLoadedObjects)  when true destroys that package's instances first, then AssetBundle.Unload and drop from cache
UnloadAllBundles(unloadAllLoadedObjects)  optional DestroyAllInstances, then unload every cached bundle and clear both caches
ClearAssetCache()  clear m_AssetCache only
LoadInEditor<T>(assetPath)  UNITY_EDITOR only; AssetDatabase.LoadAssetAtPath
BuildConfigMap() / BuildSceneMap()  private; Resources fallback when inspector fields are empty, then build the maps
LoadFromAA<T>(cacheKey, address, onLoaded) / LoadFromAB<T>(cacheKey, config, onLoaded) / LoadFromDefault<T>(cacheKey, loadKey, onLoaded)  private per-chain loaders; AB uses UnityWebRequestAssetBundle from GetBundleUrl
PruneInstances()  private; drop registry slots whose GameObject was destroyed elsewhere
GetBundleUrl(bundleName)  private static; lowercase the last path segment, join with streamingAssetsPath, file:// outside WebGL

Notes:
- PackageType selects the whole path: AA -> Addressables address, AB -> AssetBundle over UnityWebRequest keyed by bundleName, Default -> Resources path; the cache key is always config.id, so all three share one entry per package.
- Instantiation must go through m_InstanceMap, otherwise UnloadBundle(..., true) / DestroyInstances cannot destroy the instances and they survive with freed meshes (pink).
- The asset cache must be invalidated before the bundle is unloaded: LoadAssetRoutine does not test liveness on a cache hit, so a stale entry hands out a destroyed object (MissingReferenceException / blank images).
- Instantiation deliberately uses Object.Instantiate instead of Addressables.InstantiateAsync: lifetime is owned per package, the pool must be able to Destroy and reuse freely, and all three PackageTypes must behave identically.
- Global packages with an empty clipId can only be released by bundle name; an empty bundleName returns 0 without unloading, or the empty string would match unintended configs.
- Editor and runtime scene paths differ: Editor loads through EditorSceneManager (no handle, unload via SceneManager); the runtime handle must be kept or the AA scene package can never be released.
- BuildConfigMap falls back to Resources/Config/PackageDB_Master so a new package config needs no change to Manager.prefab; BuildSceneMap follows the same convention with SceneAAConfig.
- DelayInit must set isInit after building both maps, otherwise Setup's start chain waits the full 15s and times out.
- GetBundleUrl lowercases only the last path segment to match Unity's build output on case-sensitive platforms (Linux / Android / WebGL); assetPath and bundleName must not be edited for case.
- LoadFromAB caches the bundle by bundleName but the asset by config.id: names for the bundle layer, ids everywhere else.
