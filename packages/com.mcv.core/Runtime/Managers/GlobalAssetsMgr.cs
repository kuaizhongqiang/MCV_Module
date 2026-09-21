using System;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Models.Addressable;
using MCV_Module.Models.Project;
using MCV_Module.Utils;
using MCV_Module.Utils.Pool;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MCV_Module.Singleton;
using UnityEngine;
using UnityEngine.Networking;

namespace MCV_Module.Managers
{
    /// <summary>
    /// 全局资产管理器 —— 统一管理 StreamingAssets 中图片、视频等资源的异步加载，
    /// 并作为「预制体 → 池化实例」的门面（内部转调 GlobalAddressableMgr 的包管线）。
    ///
    /// 图片加载带 LRU 缓存（上限 MaxCachedImages）：
    ///   - 命中缓存直接回调（并刷新 LRU 顺序），不再重复请求
    ///   - 超上限时淘汰最久未使用的 Sprite 并 Destroy（连同其 Texture），避免内存无限增长
    /// 注意：淘汰会使持有该 Sprite 的 UI 引用失效，调用方应只短暂持有或自行复制。
    ///
    /// **内容包装卸**：本类还负责「一个 ProjectClip 一个 AB 包」的装卸时机 ——
    /// 订阅 <c>SceneStateChangeEventData</c>：进入内容页（<c>SceneState.UI</c>）加载 <c>currentClip</c> 的包，
    /// 离开则整体卸载（先卸旧再载新）；加载全程用现成的 <c>SceneLoadingEvent</c> / <c>SceneLoadedEvent</c>
    /// 驱动 LoadingPanel 遮罩，完成后发 <c>ClipReadyEvent</c> 让消费方补装配。
    ///
    /// ⚠ 包边界：卸载时还需要 module 侧（<c>InstShowManager</c> 等）释放各自的对象池，
    /// 但 **core 不能反向引用 module** —— 故通过 <see cref="PackageReleasing"/> 注入点由 module 自注册
    /// （与 <c>GlobalUIMgr.TaskPanelDescProvider</c> 同一范式）。
    /// </summary>
    public class GlobalAssetsMgr : SingletonGlobalMgr<GlobalAssetsMgr>
    {
        #region 参数
        /// <summary>图片缓存上限，超过后按最近最少使用淘汰</summary>
        const int MaxCachedImages = 32;

        readonly Dictionary<string, Sprite> imageCache = new Dictionary<string, Sprite>();
        readonly List<string> cacheOrder = new List<string>();
        #endregion

        #region 生命周期
        protected GlobalAssetsMgr() { }

        protected override IEnumerator DelayInit()
        {
            // 内容包装卸由状态事件驱动。本类在 Setup 启动链上、且早于 additive 场景加载，
            // 因此在这里订阅不会漏掉首个状态事件。
            EventBus<SceneStateChangeEventData>.Subscribe(OnSceneStateChanged);

            int configCount = GlobalAddressableMgr.Exists ? GlobalAddressableMgr.Instance.ConfigCount : 0;
            Log.Info($"[GlobalAssetsMgr] 内容包装卸已就绪（运行时包字典 {configCount} 条，等待 SceneStateChangeEventData）");

            isInit = true;
            yield break;
        }

        /// <summary>销毁时退订并清掉全局池的静态引用（关闭 Domain Reload 的编辑器下避免跨 Play 残留）。</summary>
        protected override void OnDestroy()
        {
            EventBus<SceneStateChangeEventData>.Unsubscribe(OnSceneStateChanged);
            s_Pools = null;
            base.OnDestroy();
        }
        #endregion

        #region 静态方法
        /// <summary>
        /// 从 StreamingAssets/Image/ 异步加载图片并回调 Sprite（带 LRU 缓存）
        /// </summary>
        /// <param name="imageName"> 图片文件名（如 "header.png"）</param>
        /// <param name="onSuccess"> 加载成功回调，返回 Sprite </param>
        /// <param name="onError"> 加载失败回调，返回错误信息（可选）</param>
        public static void LoadImageAsync(string imageName, Action<Sprite> onSuccess, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(imageName))
            {
                onError?.Invoke("imageName 为空");
                return;
            }

            var mgr = Instance;

            // 缓存命中：直接回调并刷新 LRU 顺序
            if (mgr.imageCache.TryGetValue(imageName, out var cached))
            {
                mgr.Touch(imageName);
                onSuccess?.Invoke(cached);
                return;
            }

            mgr.StartCoroutine(mgr.LoadImageCoroutine(imageName, onSuccess, onError));
        }
        #endregion

        #region 预制体与池化实例
        /// <summary>全局共享的预制体对象池（key = 包配置 id）；按需懒创建，容器挂在管理器下。</summary>
        static ObjectPoolMgr s_Pools;

        /// <summary>
        /// 全局共享池。适用于「没有 Inst* 管理器、但仍想复用实例」的场合（如弹层、列表项）。
        /// Inst 管理器各自持有独立池（生命周期随管理器），不走这里。
        /// </summary>
        public static ObjectPoolMgr Pools
        {
            get
            {
                if (s_Pools != null) return s_Pools;

                var mgr = Instance;
                if (mgr == null) return null;

                var root = new GameObject("GlobalPrefabPool");
                root.transform.SetParent(mgr.transform, false);
                s_Pools = new ObjectPoolMgr(root.transform);
                return s_Pools;
            }
        }

        /// <summary>
        /// 从包配置 id 异步加载预制体（走 GlobalAddressableMgr 的 AA / AB / Default 三链路 + 资源缓存）。
        /// 这是本工程**唯一的预制体加载入口**，不要直接 Resources.Load 或 AssetBundle.LoadAsset。
        /// </summary>
        public static void LoadPrefabAsync(string packageId, Action<GameObject> onSuccess, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                onError?.Invoke("[GlobalAssetsMgr] packageId 为空");
                return;
            }

            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null)
            {
                const string error = "[GlobalAssetsMgr] GlobalAddressableMgr 未就绪，预制体加载被跳过";
                Log.Error(error);
                onError?.Invoke(error);
                return;
            }

            GlobalAddressableMgr.Instance.LoadAssetAsync<GameObject>(packageId, prefab =>
            {
                if (prefab == null)
                {
                    onError?.Invoke($"[GlobalAssetsMgr] 预制体加载失败：{packageId}");
                    return;
                }
                onSuccess?.Invoke(prefab);
            });
        }

        /// <summary>
        /// 加载并池化取用：池已就绪则同步返回，否则先加载预制体再建池取用。
        /// </summary>
        /// <param name="packageId">包配置 id（对应一个预制体）</param>
        /// <param name="parent">活跃实例父节点（为空时挂在全局池容器下）</param>
        /// <param name="onSpawned">取用成功回调（返回池内实例，可能为 null）</param>
        /// <param name="onError">失败回调（管理器未就绪 / 包配置缺失 / 加载失败）</param>
        /// <param name="maxIdleCount">该池的闲置上限（&lt;=0 不限）</param>
        /// <param name="preload">首次建池后预热数量</param>
        public static void SpawnPrefabAsync(string packageId, Transform parent, Action<GameObject> onSpawned,
                                           Action<string> onError = null, int maxIdleCount = 24, int preload = 0)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                onError?.Invoke("[GlobalAssetsMgr] packageId 为空");
                return;
            }

            var pools = Pools;
            if (pools == null)
            {
                onError?.Invoke("[GlobalAssetsMgr] 全局池未就绪");
                return;
            }

            // 池已建好：直接同步取用（热路径，无加载、无 GC）
            if (pools.Contains(packageId))
            {
                onSpawned?.Invoke(pools.Spawn(packageId, parent));
                return;
            }

            LoadPrefabAsync(packageId, prefab =>
            {
                pools.CreatePool(packageId, prefab, maxIdleCount, preload, CreateTrackedInstance);
                onSpawned?.Invoke(pools.Spawn(packageId, parent));
            }, onError);
        }

        /// <summary>归还全局池中的实例（实例不属于全局池时返回 false）。</summary>
        public static bool DespawnPrefab(GameObject instance)
        {
            var pools = s_Pools;                     // 不主动创建池
            return pools != null && pools.Despawn(instance);
        }

        /// <summary>预热指定包的实例（首次会先加载预制体）。</summary>
        public static void PreloadPrefabAsync(string packageId, int count, Action<int> onComplete = null,
                                             Action<string> onError = null)
        {
            if (count <= 0)
            {
                onComplete?.Invoke(0);
                return;
            }

            var pools = Pools;
            if (pools == null)
            {
                onComplete?.Invoke(0);
                return;
            }

            var existing = pools.Get(packageId);
            if (existing != null)
            {
                onComplete?.Invoke(existing.Preload(count));
                return;
            }

            LoadPrefabAsync(packageId, prefab =>
            {
                var pool = pools.CreatePool(packageId, prefab, 0, 0, CreateTrackedInstance);
                onComplete?.Invoke(pool != null ? pool.Preload(count) : 0);
            }, onError);
        }

        /// <summary>销毁指定包的全局池（含池内实例）。</summary>
        public static void ReleasePrefabPool(string packageId, bool destroyInstances = true)
        {
            var pools = s_Pools;
            if (pools != null) pools.DestroyPool(packageId, destroyInstances);
        }

        /// <summary>池内实例的创建工厂：经 GlobalAddressableMgr 创建并登记「实例 → 包配置 id」。</summary>
        static GameObject CreateTrackedInstance(string key, GameObject prefab, Transform parent)
        {
            if (GlobalAddressableMgr.Exists && GlobalAddressableMgr.Instance != null)
                return GlobalAddressableMgr.Instance.InstantiatePrefab(prefab, key, parent);

            return UnityEngine.Object.Instantiate(prefab, parent);
        }
        #endregion

        #region 内容包装卸（一个 ProjectClip 一个 AB 包，事件驱动）
        /// <summary>单个资源的加载超时（秒）。</summary>
        const float AssetLoadTimeout = 10f;

        /// <summary>
        /// 「内容包即将被卸载」的扩展点：**module 侧自注册**（Inst* 管理器释放自己的对象池 ——
        /// 池持有 bundle 内预制体的引用，必须在卸 bundle 之前释放，否则池不会用新预制体重建、
        /// 再 Spawn 会抛 MissingReferenceException）。
        ///
        /// 为什么用注入点：本类属 core 包，Inst* 管理器属 module 包，core 直接引用 module 会形成
        /// 反向依赖（包化形态下编译失败）。
        /// </summary>
        public static event Action<string, bool> PackageReleasing;

        /// <summary>已完成加载的 clip（幂等去重）。</summary>
        string loadedClipId;

        /// <summary>正在加载的 clip（在途去重 —— 同一 clip 重入直接返回）。</summary>
        string loadingClipId;

        /// <summary>装卸代次：异步回调只认最新一次（加载期间切器件 / 退出内容页会作废在途结果）。</summary>
        int contentToken;

        /// <summary>
        /// 某个 clip 的包是否已全部加载完成（面板可据此决定要不要占位 / 等 ClipReadyEvent）。
        /// **无内容配置的 clip 也会记为就绪** —— 它没有包可等，消费方应直接走同步装配。
        /// </summary>
        public static bool IsClipReady(string clipId)
        {
            var mgr = Instance;
            return mgr != null && !string.IsNullOrEmpty(clipId) && mgr.loadedClipId == clipId;
        }

        /// <summary>按**包配置 id** 同步取已加载的 Sprite（未就绪返回 null，不触发加载）。</summary>
        public static Sprite GetSpriteByPackageId(string packageId)
        {
            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null) return null;
            GlobalAddressableMgr.Instance.TryGetCached<Sprite>(packageId, out var sprite);
            return sprite;
        }

        /// <summary>按**包配置 id** 同步取已加载的预制体（未就绪返回 null，不触发加载）。</summary>
        public static GameObject GetPrefabByPackageId(string packageId)
        {
            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null) return null;
            GlobalAddressableMgr.Instance.TryGetCached<GameObject>(packageId, out var prefab);
            return prefab;
        }

        /// <summary>
        /// 按包配置 id 列表同步取 Sprite（保序）。返回数量少于入参即说明还没加载完
        /// —— 消费方应等 <c>ClipReadyEvent</c> 再取一次。
        /// </summary>
        public static List<Sprite> GetSpritesByPackageIds(IList<string> packageIds)
        {
            var list = new List<Sprite>();
            if (packageIds == null) return list;

            for (int i = 0; i < packageIds.Count; i++)
            {
                Sprite sprite = GetSpriteByPackageId(packageIds[i]);
                if (sprite != null) list.Add(sprite);
            }
            return list;
        }

        /// <summary>
        /// 加载一个 clip 的全部资源（保序、串行）；加载期间用 LoadingPanel 遮罩，
        /// 全部就绪后**先发 <c>ClipReadyEvent</c>、再关遮罩**（保证遮罩关掉时画面已有内容）。
        /// <para>
        /// <paramref name="onComplete"/> 的 <c>false</c> 只代表**真失败**（管理器未就绪 / 代次作废）；
        /// 「该 clip 没有内容配置」（空资源 clip）视为成功，回 <c>true</c> 且不遮罩、不发就绪事件。
        /// </para>
        /// </summary>
        public static void LoadClipAsync(string clipId, Action<bool> onComplete)
        {
            var mgr = Instance;
            if (mgr == null)
            {
                Log.Error("[GlobalAssetsMgr] 单例不存在，LoadClipAsync 被跳过");
                onComplete?.Invoke(false);
                return;
            }
            mgr.StartCoroutine(mgr.LoadClipRoutine(clipId, mgr.contentToken, onComplete));
        }

        IEnumerator LoadClipRoutine(string clipId, int token, Action<bool> onComplete)
        {
            loadingClipId = clipId;

            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null)
            {
                Log.Error("[GlobalAssetsMgr] GlobalAddressableMgr 未就绪，内容包加载中止");
                loadingClipId = null;
                onComplete?.Invoke(false);
                yield break;
            }

            List<ABPackageConfigSO> configs = GlobalAddressableMgr.Instance.GetConfigsByClip(clipId);
            if (configs.Count == 0)
            {
                // 预期内状态：无资源的 clip（四步全 null）不产包，故降到 Verbose，避免刷误导性红字。
                // 按「加载成功但无内容」处理，不能回 false：调用方对 !ok 一律打 Error，无包 clip 会被误报成失败。
                // 同步记入 loadedClipId（本分支在首个 yield 之前，保持同帧生效）：
                //   ① 同 clip 重入走幂等短路；② IsClipReady 为真 —— 消费方走「同步装配」，
                //      不会因为没有 ClipReadyEvent 而一直挂着。
                Log.Verbose($"[GlobalAssetsMgr] {clipId} 没有内容配置（无资源 clip 属预期，视为就绪）。" +
                            $"字典里现有 clip：[{string.Join(", ", GlobalAddressableMgr.Instance.GetClipIds())}]");
                loadingClipId = null;
                loadedClipId = clipId;
                onComplete?.Invoke(true);
                yield break;
            }

            Log.Info($"[GlobalAssetsMgr] 开始加载内容包：{clipId}（{configs.Count} 项）");

            // 首个遮罩事件延后一帧：SceneStateChangeEventData 是同步分发，而 GlobalUIMgr 换 Canvas 是协程
            // （先淡出旧 Canvas，再 ClearPanels + Rebuild）。同帧发会让遮挡层先建在正在淡出的旧 Canvas 上。
            yield return null;
            EventBus<SceneLoadingEvent>.Publish(new SceneLoadingEvent(clipId) { Progress = 0f });

            int done = 0;
            for (int i = 0; i < configs.Count; i++)
            {
                // 已切走（代次过期）：停止继续加载与推进进度，免得两路协程交错刷遮罩进度
                if (token != contentToken) break;

                ABPackageConfigSO config = configs[i];
                string id = config.id;
                bool finished = false;

                // **按资源类型分流加载（必须）**：
                //   `.png` 导入为 Sprite 时「主资产是 Texture2D、Sprite 是子资产」，
                //   用 Object 泛型加载只会拿到 Texture2D，之后 as Sprite 永远为 null（图集一片空白）；
                //   `.prefab` 的主资产就是 GameObject，用 GameObject 泛型取。
                if (config.assetKind == ContentAssetKind.Sprite)
                    GlobalAddressableMgr.Instance.LoadAssetAsync<Sprite>(id, _ => finished = true);
                else
                    GlobalAddressableMgr.Instance.LoadAssetAsync<GameObject>(id, _ => finished = true);

                float wait = 0f;
                while (!finished && wait < AssetLoadTimeout)
                {
                    wait += Time.deltaTime;
                    yield return null;
                }
                if (!finished) Log.Error($"[GlobalAssetsMgr] 资源加载超时：{id}");

                done++;
                EventBus<SceneLoadingEvent>.Publish(new SceneLoadingEvent(clipId) { Progress = (float)done / configs.Count });
            }

            // 代次校验必须留在写共享状态之前：加载途中用户又切了器件 / 退出内容页时，本次结果作废
            // —— 不写 loadedClipId、不发就绪事件、不动遮罩。否则会覆盖新一次加载的状态（泄漏）。
            if (token != contentToken)
            {
                Log.Info($"[GlobalAssetsMgr] {clipId} 的加载结果已作废（期间切换了器件或退出内容页）");
                onComplete?.Invoke(false);
                yield break;
            }

            loadingClipId = null;
            loadedClipId = clipId;

            Log.Info($"[GlobalAssetsMgr] 内容包就绪：{clipId}（{configs.Count} 项）");

            // 顺序不能反：先让消费方补装配，再关遮罩
            EventBus<ClipReadyEvent>.Publish(new ClipReadyEvent(clipId));
            EventBus<SceneLoadedEvent>.Publish(new SceneLoadedEvent(clipId));

            onComplete?.Invoke(true);
        }

        /// <summary>
        /// 状态事件驱动装卸：**只在内容页态（SceneState.UI）加载，其余态卸载**；一次只维护一个包；
        /// 同态内换了 ProjectClip 则「先卸旧、再载新」。
        ///
        /// 刻意**不订阅** <c>TaskTypeChangeEventData</c>：它在 MenuController 里早于状态事件发布（此刻还在菜单态），
        /// 按它加载会提前 IO，且紧随其后的状态事件会因 loadedClipId 未写而判成「不同 clip」重复加载。
        /// </summary>
        void OnSceneStateChanged(SceneStateChangeEventData e)
        {
            if (e == null) return;

            if (e.State != SceneState.UI)
            {
                contentToken++;                  // 作废在途加载

                // **只有「加载途中被切走」才需要补关遮罩**（此时加载协程已作废，不会再发 SceneLoadedEvent）。
                // 已加载完成时遮罩早就关了；从未加载过的状态更不能发 ——
                // 那些状态下 LoadingPanel 往往处于 inactive，LoadingController 去 SetUIActive(false)
                // 会在 inactive 的 GameObject 上 StartCoroutine 抛
                // "Coroutine couldn't be started because the gameObject is inactive"。
                bool needHideMask = !string.IsNullOrEmpty(loadingClipId);

                UnloadCurrentClip();

                if (needHideMask) EventBus<SceneLoadedEvent>.Publish(new SceneLoadedEvent(e.State.ToString()));
                return;
            }

            ProjectClip clip = GlobalDataMgr.GetProjectClip();
            if (clip == null)
            {
                Log.Verbose("[GlobalAssetsMgr] 进入内容页但 currentClip 为空，跳过内容包加载");
                return;
            }

            if (clip.id == loadedClipId || clip.id == loadingClipId) return;   // 幂等 / 在途去重

            contentToken++;
            int token = contentToken;

            UnloadCurrentClip();                 // 切器件：先卸旧

            LoadClipAsync(clip.id, ok =>
            {
                if (token != contentToken)
                {
                    // 过期结果：加载期间又切了器件 / 退出内容页 → 立刻把这个包也卸掉，
                    // 避免"卸载后又被装回来"
                    if (GlobalAddressableMgr.Exists && GlobalAddressableMgr.Instance != null)
                        GlobalAddressableMgr.Instance.UnloadClip(clip.id);
                    loadedClipId = null;
                    return;
                }

                if (!ok) Log.Error($"[GlobalAssetsMgr] 内容包加载失败：{clip.id}");
            });
        }

        /// <summary>
        /// 整体卸载当前 clip（**卸载三步，顺序不能变**）：
        /// ① 释放该 clip 各配置 id 的对象池 —— 池持有 bundle 内预制体的引用，必须在卸 bundle 前释放，
        ///    否则池不会用新预制体重建 → 再 Spawn 抛 MissingReferenceException；
        /// ② 失效资源缓存 + ③ 卸 bundle（连带销毁属于该包的存活实例）—— 由 GlobalAddressableMgr.UnloadClip 完成。
        /// </summary>
        void UnloadCurrentClip()
        {
            string clipId = loadedClipId;
            loadedClipId = null;
            loadingClipId = null;

            if (string.IsNullOrEmpty(clipId)) return;
            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null) return;

            List<ABPackageConfigSO> configs = GlobalAddressableMgr.Instance.GetConfigsByClip(clipId);

            // ① 对象池：本类的全局池 + module 侧各 Inst* 管理器的池（后者经注入点回调）
            foreach (ABPackageConfigSO config in configs)
            {
                ReleasePrefabPool(config.id, true);
                PackageReleasing?.Invoke(config.id, true);
            }

            // ② 失效缓存 + ③ 卸 bundle
            GlobalAddressableMgr.Instance.UnloadClip(clipId);

            Log.Info($"[GlobalAssetsMgr] 已卸载内容包：{clipId}（{configs.Count} 项）");
        }
        #endregion

        #region 私有方法
        IEnumerator LoadImageCoroutine(string imageName, Action<Sprite> onSuccess, Action<string> onError)
        {
            var imagePath = Path.Combine("Image", imageName);
            var url = GetStreamingUrl(imagePath);

            using (var uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    var texture = DownloadHandlerTexture.GetContent(uwr);
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                    Log.Info($"[GlobalAssetsMgr] 图片加载成功：{imagePath}");

                    // 并发加载同一图片时，CacheImage 会销毁重复的，返回生效的那份
                    Sprite effective = CacheImage(imageName, sprite);
                    onSuccess?.Invoke(effective);
                }
                else
                {
                    var error = $"[GlobalAssetsMgr] 加载图片失败: {url}, {uwr.error}";
                    Log.Error(error);
                    onError?.Invoke(error);
                }
            }
        }

        /// <summary>写入缓存；若已存在（并发重复）则销毁新加载的并返回已有缓存。</summary>
        Sprite CacheImage(string imageName, Sprite sprite)
        {
            if (imageCache.TryGetValue(imageName, out var existing))
            {
                if (sprite != null && sprite != existing)
                {
                    if (sprite.texture != null) Destroy(sprite.texture);
                    Destroy(sprite);
                }
                return existing;
            }

            imageCache[imageName] = sprite;
            cacheOrder.Add(imageName);
            EvictIfNeeded();
            return sprite;
        }

        /// <summary>刷新 LRU 顺序（移到队尾 = 最近使用）</summary>
        void Touch(string imageName)
        {
            cacheOrder.Remove(imageName);
            cacheOrder.Add(imageName);
        }

        /// <summary>超出上限时淘汰最久未使用的图片（Destroy Sprite + Texture）</summary>
        void EvictIfNeeded()
        {
            while (cacheOrder.Count > MaxCachedImages)
            {
                string oldest = cacheOrder[0];
                cacheOrder.RemoveAt(0);
                if (imageCache.TryGetValue(oldest, out var sprite))
                {
                    imageCache.Remove(oldest);
                    if (sprite != null)
                    {
                        if (sprite.texture != null) Destroy(sprite.texture);
                        Destroy(sprite);
                        Log.Info($"[GlobalAssetsMgr] 缓存淘汰：{oldest}");
                    }
                }
            }
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 构造 StreamingAssets 完整路径（WebGL 兼容）
        /// </summary>
        static string GetStreamingUrl(string relativePath)
        {
            var path = Path.Combine(Application.streamingAssetsPath, relativePath);
#if UNITY_WEBGL && !UNITY_EDITOR
            return path;
#else
            return "file:///" + path;
#endif
        }
        #endregion
    }
}
