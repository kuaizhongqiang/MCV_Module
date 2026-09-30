# Contract: GlobalAssetsMgr

Role: global asset facade; async image loading with an LRU sprite cache, prefab pooling on top of ObjectPoolMgr, and event-driven load/unload of one AssetBundle per ProjectClip.

Fields:
MaxCachedImages:int  const 32; sprite cache cap, eviction destroys the Sprite plus its Texture
imageCache:Dictionary<string, Sprite>  readonly; keyed by image file name
cacheOrder:List<string>  readonly; LRU order, index 0 = least recently used
AssetLoadTimeout:float  const 10; per-asset load timeout in seconds
GlobalAssetLoadTimeout:float  const 10; per-asset timeout inside the global-package preload
loadedClipId:string  the clip whose packages are fully loaded (idempotent short-circuit)
loadingClipId:string  the clip currently in flight (re-entry for the same clip returns at once)
contentToken:int  load generation; an async callback is honoured only for the latest generation
s_Pools:ObjectPoolMgr  static; lazily created global prefab pool keyed by package-config id, container parented under this manager

Methods:
DelayInit()  subscribe SceneStateChangeEventData -> log ready -> isInit = true
OnDestroy()  unsubscribe -> s_Pools = null -> base.OnDestroy()
LoadImageAsync(imageName, onSuccess, onError = null)  static; empty name -> onError; cache hit -> Touch + onSuccess; else LoadImageCoroutine
LoadSpritesByPackageIdsAsync(packageIds, onSuccess, onError = null)  static; empty input -> onSuccess(empty); manager not ready -> onError; else GlobalAddressableMgr.LoadAssetsAsync<Sprite>; warns when fewer items come back
IsClipReady(clipId)  static; loadedClipId == clipId (a clip with no config also counts as ready)
GetSpriteByPackageId(packageId) / GetPrefabByPackageId(packageId)  static; TryGetCached reads, never trigger a load, null when not ready
GetFontByFontId(fontId) / GetTmpFontAssetByFontId(fontId)  static (B3); TryGetCached<Font> / TryGetCached<TMP_FontAsset> on ContentNaming.FontAssetId(fontId, FontSlotLegacy / FontSlotTmp), never trigger a load, null when not ready
GetSpritesByPackageIds(packageIds)  static; order-preserving sync fetch, null entries skipped; a short list means still loading
LoadClipAsync(clipId, onComplete)  static; no singleton -> error + onComplete(false); else LoadClipRoutine with the current contentToken
LoadClipRoutine(clipId, token, onComplete)  coroutine; set loadingClipId -> empty-config branch -> one frame + SceneLoadingEvent progress per config -> generation check -> ClipReadyEvent then SceneLoadedEvent -> onComplete(true)
PreloadGlobalBundleAsync(bundleName, onComplete)  static; preload every config of one global package, generic split by assetKind (Sprite / Font / TmpFont / otherwise GameObject); 0 configs counts as failure and logs an Error
OnSceneStateChanged(e)  non-UI -> contentToken++ -> conditional SceneLoadedEvent -> UnloadCurrentClip; UI -> dedup -> contentToken++ -> UnloadCurrentClip -> LoadClipAsync, and a stale result unloads the clip that just landed
UnloadCurrentClip()  clear both ids -> ReleasePrefabPool(config.id, true) + InstShowManager.Instance.ReleasePackage(config.id, true) -> GlobalAddressableMgr.Instance.UnloadClip(clipId)
Pools  static; lazily creates a "GlobalPrefabPool" GameObject under this manager and wraps it in ObjectPoolMgr
LoadPrefabAsync(packageId, onSuccess, onError = null)  static; empty id / manager not ready -> onError; else LoadAssetAsync<GameObject>, null prefab -> onError
SpawnPrefabAsync(packageId, parent, onSpawned, onError = null, maxIdleCount = 24, preload = 0)  static; pool exists -> sync Spawn; else load -> CreatePool -> Spawn
DespawnPrefab(instance)  static; uses s_Pools only, never creates a pool; false when the instance is not pooled
PreloadPrefabAsync(packageId, count, onComplete = null, onError = null)  static; count <= 0 -> onComplete(0); existing pool -> Preload; else load -> CreatePool -> Preload
ReleasePrefabPool(packageId, destroyInstances = true)  static; DestroyPool on s_Pools when it exists
CreateTrackedInstance(key, prefab, parent)  static pool factory; InstantiatePrefab (registers instance -> package id), falls back to Object.Instantiate
LoadImageCoroutine(imageName, onSuccess, onError)  coroutine; UnityWebRequestTexture -> Sprite.Create -> CacheImage -> onSuccess; failure -> Log.Error + onError
CacheImage(imageName, sprite)  on a concurrent duplicate destroys the newcomer (Sprite + Texture) and returns the live one
Touch(imageName) / EvictIfNeeded()  move the key to the end / destroy the oldest Sprite + Texture past MaxCachedImages
GetStreamingUrl(relativePath)  static; Path.Combine with streamingAssetsPath; WebGL non-editor returns the raw path, otherwise "file:///"

Notes:
- Eviction destroys both the Sprite and its Texture, so any UI still holding the reference goes invalid; callers should hold it briefly or copy it.
- A cache hit must refresh the LRU order, otherwise a hot image is evicted as the oldest.
- The package path (LoadSpritesByPackageIdsAsync / GetSpriteByPackageId / GetPrefabByPackageId) goes through GlobalAddressableMgr: the Sprite belongs to its AssetBundle, so there is no LRU and no manual Destroy; release goes through UnloadBundle / UnloadAllBundles / UnloadClip.
- Generic loading must follow ABPackageConfigSO.assetKind: for a .png the main asset is Texture2D and the Sprite is a sub-asset, so an Object generic returns only Texture2D and "as Sprite" stays null (blank atlas). B3 extended the same split inside PreloadGlobalBundleRoutine with Font (UnityEngine.Font) and TmpFont (TMPro.TMP_FontAsset); a font loaded with the GameObject generic would come back null without any error.
- The font facades (GetFontByFontId / GetTmpFontAssetByFontId) are synchronous reads that never start a load and return null when the font bundle is missing or not preloaded yet. Callers MUST degrade on null by leaving the node's existing font alone: assigning null would make the text disappear, which is exactly the failure the font catalog's "paths only, no references" design is meant to survive.
- LoadClipAsync cannot be reused for a global package: it collects its configs through GetConfigsByClip, and a global package keeps clipId empty, so it would never collect anything (PreloadGlobalBundleAsync is the by-bundleName entry).
- A clip with no config (e.g. clip_quiz) counts as ready: LoadClipRoutine returns true and records loadedClipId in the same frame (the branch sits before the first yield) without a mask. Returning false would make OnSceneStateChanged log a misleading error on the quiz/exam pages.
- The generation check must stay before any shared state is written: otherwise a stale routine overwrites loadedClipId and the newly loaded bundle is never unloaded (leak). On a stale result the callback also unloads the clip that just landed, so it cannot be "installed back after unload".
- The first SceneLoadingEvent is delayed by one frame: SceneStateChangeEventData is dispatched synchronously while GlobalUIMgr swaps canvases in a coroutine, so the same frame would attach the mask to the fading old canvas.
- ClipReadyEvent is published before the mask goes off (SceneLoadedEvent), so consumers can pick assets synchronously by the time the mask disappears.
- Do not subscribe TaskTypeChangeEventData: it is published by MenuController before the state event (still in menu state), so loading on it does early IO and the following state event loads the same clip again.
- SceneLoadedEvent on leaving the content page is published only while loadingClipId is set; other states have an inactive LoadingPanel and StartCoroutine on it throws.
- Unload order per config is ReleasePrefabPool + InstShowManager.ReleasePackage first, then UnloadClip: the pool holds prefab references inside the bundle, and unloading first would leave the pool unable to rebuild from the new prefab (Spawn throws MissingReferenceException).
- LoadPrefabAsync is the only prefab load entry point; Resources.Load / AssetBundle.LoadAsset bypass instance registration and unloading.
- OnSceneStateChanged is subscribed in DelayInit because this manager sits on the Setup chain, earlier than additive scene loads, so no state event is missed.
- s_Pools is nulled in OnDestroy to avoid leftovers between Play sessions in the editor with Domain Reload disabled.
