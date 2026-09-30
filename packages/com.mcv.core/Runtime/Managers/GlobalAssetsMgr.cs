using System;
using MCV_Module.Event;
using MCV_Module.Managers.InstManagers;
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
    /// <summary>全局资产门面：图片 LRU 缓存、预制体池化，并按状态事件装卸「一 clip 一包」内容包。</summary>
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
            // WHY: 本类在 Setup 启动链上且早于 additive 场景加载，在此订阅才不会漏掉首个状态事件
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
        /// <summary>从 StreamingAssets/Image/ 异步加载图片并回调 Sprite（带 LRU 缓存）</summary>
        public static void LoadImageAsync(string imageName, Action<Sprite> onSuccess, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(imageName))
            {
                onError?.Invoke("imageName 为空");
                return;
            }

            var mgr = Instance;

            // WHY: 命中缓存也必须刷新 LRU 顺序，否则热点图会被当成最久未用而淘汰
            if (mgr.imageCache.TryGetValue(imageName, out var cached))
            {
                mgr.Touch(imageName);
                onSuccess?.Invoke(cached);
                return;
            }

            mgr.StartCoroutine(mgr.LoadImageCoroutine(imageName, onSuccess, onError));
        }

        // WHY: 这里的 Sprite 归 AssetBundle 持有，不能像 LoadImageAsync 那样 LRU 淘汰或 Destroy，释放只能走 UnloadBundle / UnloadAllBundles
        /// <summary>按「包配置 id」批量加载图片（一个器件一个包内多张图），走包管线</summary>
        public static void LoadSpritesByPackageIdsAsync(IList<string> packageIds, Action<List<Sprite>> onSuccess, Action<string> onError = null)
        {
            if (packageIds == null || packageIds.Count == 0)
            {
                onSuccess?.Invoke(new List<Sprite>());
                return;
            }

            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null)
            {
                var error = "[GlobalAssetsMgr] GlobalAddressableMgr 未就绪，图集加载被跳过";
                Log.Error(error);
                onError?.Invoke(error);
                return;
            }

            GlobalAddressableMgr.Instance.LoadAssetsAsync<Sprite>(packageIds, sprites =>
            {
                if (sprites == null) sprites = new List<Sprite>();
                if (sprites.Count != packageIds.Count)
                {
                    Log.Warning($"[GlobalAssetsMgr] 图集加载不完整：请求 {packageIds.Count} 张，实际 {sprites.Count} 张");
                }
                onSuccess?.Invoke(sprites);
            });
        }
        #endregion

        #region 内容包装卸（一个 ProjectClip 一个 AB 包，事件驱动）
        /// <summary>单个资源的加载超时（秒）。</summary>
        const float AssetLoadTimeout = 10f;

        /// <summary>已完成加载的 clip（幂等去重）。</summary>
        string loadedClipId;

        /// <summary>正在加载的 clip（在途去重 —— 同一 clip 重入直接返回）。</summary>
        string loadingClipId;

        /// <summary>装卸代次：异步回调只认最新一次（加载期间切器件 / 退出内容页会作废在途结果）。</summary>
        int contentToken;

        // WHY: 无内容配置的 clip（如 clip_quiz）也算就绪——它没有包可等，消费方须直接走同步装配
        /// <summary>某个 clip 的包是否已全部加载完成（面板据此决定是否占位 / 等 ClipReadyEvent）</summary>
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

        // WHY: 取不到**必须**让调用方降级 —— 绝对不要拿 null 去覆盖节点上已有的字体：字体包缺 / 未预加载 / 预加载还在途时，
        // 拿 null 赋值会让文本直接消失。所以这里未就绪只回 null，由调用方选择「不动原有字体」（TextComponent 就是这么做的）。
        /// <summary>按 fontId 同步取已加载的 Legacy 字体（B3；未就绪返回 null，不触发加载）。</summary>
        public static Font GetFontByFontId(string fontId)
        {
            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null) return null;
            GlobalAddressableMgr.Instance.TryGetCached<Font>(
                ContentNaming.FontAssetId(fontId, ContentNaming.FontSlotLegacy), out var font);
            return font;
        }

        /// <summary>按 fontId 同步取已加载的 TMP 字体资产（B3；未就绪返回 null，不触发加载）。降级口径同 <see cref="GetFontByFontId"/>。</summary>
        public static TMPro.TMP_FontAsset GetTmpFontAssetByFontId(string fontId)
        {
            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null) return null;
            GlobalAddressableMgr.Instance.TryGetCached<TMPro.TMP_FontAsset>(
                ContentNaming.FontAssetId(fontId, ContentNaming.FontSlotTmp), out var tmpFont);
            return tmpFont;
        }

        // WHY: 本工程把「运行时自建材质」所需的 shader 放进 Assets/Resources/Shaders（default 包），不走 AB 引用链。
        //       理由：shader 若只靠 AB 内 prefab 的材质引用间接带上，包内会再生出一份 shader 实例，运行期 new Material(shader) 用的就是那份包内副本；
        //       放进 Resources 后运行期取到的是同一份工程资产，与 AB 内那份副本解耦（模型预览面板即按此重构，见 PreviewRig）。
        //       注意这是**刻意的**取舍：shader 进 default 包（体积仅几 KB），换取向来由代码驱动的材质/RT 全部自持。
        /// <summary>按 Resources 路径同步取运行时自建材质用的 shader（取不到返回 null，调用方须降级不建材质）。</summary>
        public static Shader GetRuntimeShader(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;

            Shader shader = Resources.Load<Shader>(resourcePath);
            if (shader == null)
                Log.Error($"[GlobalAssetsMgr] Resources 取 shader 失败：{resourcePath}（须位于 Assets/Resources/ 下；缺失会导致运行时自建材质失败）");

            return shader;
        }

        // WHY: 数量少于入参只说明还没加载完，消费方应等 ClipReadyEvent 再取一次
        /// <summary>按包配置 id 列表同步取 Sprite（保序，数量少于入参即说明还没加载完）</summary>
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

        // WHY: 先发 ClipReadyEvent 再关遮罩；无内容配置的 clip 视为成功回 true，回 false 会被上层当失败刷红字
        /// <summary>加载一个 clip 的全部资源（保序、串行），加载期间用 LoadingPanel 遮罩</summary>
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
                // WHY: 无内容配置的 clip 属预期（按 BundlePipeline.md §5 不产包），必须回 true 并同步记 loadedClipId——回 false 会被上层误报失败，不记则重入会重复加载、消费方也一直等不到 ClipReadyEvent
                Log.Verbose($"[GlobalAssetsMgr] {clipId} 没有内容配置（无资源 clip 属预期，视为就绪）。" +
                            $"字典里现有 clip：[{string.Join(", ", GlobalAddressableMgr.Instance.GetClipIds())}]");
                loadingClipId = null;
                loadedClipId = clipId;
                onComplete?.Invoke(true);
                yield break;
            }

            Log.Info($"[GlobalAssetsMgr] 开始加载内容包：{clipId}（{configs.Count} 项）");

            // WHY: 遮罩事件延后一帧——SceneStateChangeEventData 同步分发而 GlobalUIMgr 换 Canvas 是协程，同帧发会先建在正在淡出的旧 Canvas 上
            yield return null;
            EventBus<SceneLoadingEvent>.Publish(new SceneLoadingEvent(clipId) { Progress = 0f });

            int done = 0;
            for (int i = 0; i < configs.Count; i++)
            {
                // WHY: 代次过期即停，继续推进会让两路协程交错刷遮罩进度
                if (token != contentToken) break;

                ABPackageConfigSO config = configs[i];
                string id = config.id;
                bool finished = false;

                // WHY: 必须按 config.assetKind 分流泛型——`.png` 的 Sprite 是子资产，用 Object 泛型只会拿到 Texture2D，之后 as Sprite 恒为 null（图集一片空白）
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

            // WHY: 代次校验必须留在写共享状态之前，否则会覆盖新一次加载的记录——新包已加载却失去记录，退出内容页时不会被卸载，属泄漏
            if (token != contentToken)
            {
                Log.Info($"[GlobalAssetsMgr] {clipId} 的加载结果已作废（期间切换了器件或退出内容页）");
                onComplete?.Invoke(false);
                yield break;
            }

            loadingClipId = null;
            loadedClipId = clipId;

            Log.Info($"[GlobalAssetsMgr] 内容包就绪：{clipId}（{configs.Count} 项）");

            // WHY: 顺序不能反：先让消费方补装配，再关遮罩
            EventBus<ClipReadyEvent>.Publish(new ClipReadyEvent(clipId));
            EventBus<SceneLoadedEvent>.Publish(new SceneLoadedEvent(clipId));

            onComplete?.Invoke(true);
        }

        // WHY: 刻意不订阅 TaskTypeChangeEventData——它早于状态事件发布（还在菜单态），按它加载会提前 IO 又被随后的状态事件重复加载
        /// <summary>状态事件驱动装卸：仅在 SceneState.UI 加载，其余态卸载；同态换 clip 先卸旧再载新</summary>
        void OnSceneStateChanged(SceneStateChangeEventData e)
        {
            if (e == null) return;

            if (e.State != SceneState.UI)
            {
                contentToken++;                  // WHY: 递增代次以作废在途加载，否则其回调会覆盖新状态

                // WHY: 只有「加载途中被切走」才补关遮罩——其他状态下 LoadingPanel 多为 inactive，去 SetUIActive(false) 会在 inactive 对象上 StartCoroutine 抛异常
                bool needHideMask = !string.IsNullOrEmpty(loadingClipId);

                UnloadCurrentClip();

                if (needHideMask) EventBus<SceneLoadedEvent>.Publish(new SceneLoadedEvent(e.State.ToString()));
                return;
            }

            ProjectClip clip = GlobalDataMgr.GetProjectClip();
            if (clip == null)
            {
                // WHY: 初始态是 Start，正常走不到这里；用 Verbose 避免未来路径变化时刷误导性 Warning
                Log.Verbose("[GlobalAssetsMgr] 进入内容页但 currentClip 为空，跳过内容包加载");
                return;
            }

            if (clip.id == loadedClipId || clip.id == loadingClipId) return;   // WHY: 幂等 / 在途去重，重复加载会重发遮罩

            contentToken++;
            int token = contentToken;

            UnloadCurrentClip();                 // WHY: 切器件要先卸旧，否则旧包与旧池实例残留

            LoadClipAsync(clip.id, ok =>
            {
                if (token != contentToken)
                {
                    // WHY: 过期结果要立刻把刚落地的包卸掉，避免「卸载后又被装回来」
                    if (GlobalAddressableMgr.Exists && GlobalAddressableMgr.Instance != null)
                        GlobalAddressableMgr.Instance.UnloadClip(clip.id);
                    loadedClipId = null;
                    return;
                }

                if (!ok) Log.Error($"[GlobalAssetsMgr] 内容包加载失败：{clip.id}");
            });
        }

        // WHY: 必须先释放对象池再卸 bundle——池持有包内预制体引用，顺序反了池不会用新预制体重建，再 Spawn 抛 MissingReferenceException
        /// <summary>整体卸载当前 clip（释放对象池 → 失效资源缓存 → 卸 bundle，顺序不能变）</summary>
        void UnloadCurrentClip()
        {
            string clipId = loadedClipId;
            loadedClipId = null;
            loadingClipId = null;

            if (string.IsNullOrEmpty(clipId)) return;
            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null) return;

            List<ABPackageConfigSO> configs = GlobalAddressableMgr.Instance.GetConfigsByClip(clipId);

            // WHY: 对象池必须先于卸 bundle 释放（池持有包内预制体引用）；将来新增 Inst* 管理器要在此登记
            foreach (ABPackageConfigSO config in configs)
            {
                ReleasePrefabPool(config.id, true);

                if (InstShowManager.Exists && InstShowManager.Instance != null)
                    InstShowManager.Instance.ReleasePackage(config.id, true);
            }

            // ② 失效缓存 + ③ 卸 bundle
            GlobalAddressableMgr.Instance.UnloadClip(clipId);

            Log.Info($"[GlobalAssetsMgr] 已卸载内容包：{clipId}（{configs.Count} 项）");
        }
        #endregion

        #region 全局包预加载（clipId 为空，按 clip 的装卸链路带不走它）
        /// <summary>全局包单个资源的加载超时（秒）。</summary>
        const float GlobalAssetLoadTimeout = 10f;

        // WHY: 全局包没有 clipId，走不了 LoadClipRoutine（它按 GetConfigsByClip 收集）；UI 包必须在首个面板创建前就绪。
        /// <summary>预加载一个全局包的全部配置（按 assetKind 分流泛型）；无配置视为失败并告警。</summary>
        public static void PreloadGlobalBundleAsync(string bundleName, Action<bool> onComplete)
        {
            var mgr = Instance;
            if (mgr == null)
            {
                Log.Error("[GlobalAssetsMgr] 单例不存在，PreloadGlobalBundleAsync 被跳过");
                onComplete?.Invoke(false);
                return;
            }
            mgr.StartCoroutine(mgr.PreloadGlobalBundleRoutine(bundleName, onComplete));
        }

        IEnumerator PreloadGlobalBundleRoutine(string bundleName, Action<bool> onComplete)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                onComplete?.Invoke(false);
                yield break;
            }

            if (!GlobalAddressableMgr.Exists || GlobalAddressableMgr.Instance == null)
            {
                Log.Error($"[GlobalAssetsMgr] GlobalAddressableMgr 未就绪，全局包 {bundleName} 预加载中止");
                onComplete?.Invoke(false);
                yield break;
            }

            List<ABPackageConfigSO> configs = GlobalAddressableMgr.Instance.GetConfigsByBundleName(bundleName);
            if (configs.Count == 0)
            {
                // WHY: 0 条通常意味着漏跑了该包的流水线（配置没写进 PackageDB_Master），必须报 Error 而不是静默成功
                Log.Error($"[GlobalAssetsMgr] 全局包 {bundleName} 没有任何包配置（是否漏跑 MCV Build 的对应流水线 / 清单未收录？）");
                onComplete?.Invoke(false);
                yield break;
            }

            Log.Info($"[GlobalAssetsMgr] 开始预加载全局包：{bundleName}（{configs.Count} 项）");

            for (int i = 0; i < configs.Count; i++)
            {
                ABPackageConfigSO config = configs[i];
                bool finished = false;

                // WHY: 必须按 config.assetKind 分流泛型——`.png` 的 Sprite 是子资产，用 Object 泛型只会拿到 Texture2D；
                // 字体两种形态（B3）也要各用自己的泛型，否则字体包预加载会拿到 null 且不报错（配置在、资源取不到）。
                if (config.assetKind == ContentAssetKind.Sprite)
                    GlobalAddressableMgr.Instance.LoadAssetAsync<Sprite>(config, _ => finished = true);
                else if (config.assetKind == ContentAssetKind.Font)
                    GlobalAddressableMgr.Instance.LoadAssetAsync<Font>(config, _ => finished = true);
                else if (config.assetKind == ContentAssetKind.TmpFont)
                    GlobalAddressableMgr.Instance.LoadAssetAsync<TMPro.TMP_FontAsset>(config, _ => finished = true);
                else
                    GlobalAddressableMgr.Instance.LoadAssetAsync<GameObject>(config, _ => finished = true);

                float wait = 0f;
                while (!finished && wait < GlobalAssetLoadTimeout)
                {
                    wait += Time.deltaTime;
                    yield return null;
                }
                if (!finished) Log.Error($"[GlobalAssetsMgr] 全局包 {bundleName} 资源加载超时：{config.id}");
            }

            Log.Info($"[GlobalAssetsMgr] 全局包就绪：{bundleName}（{configs.Count} 项）");
            onComplete?.Invoke(true);
        }
        #endregion

        #region 预制体与池化实例
        /// <summary>全局共享的预制体对象池（key = 包配置 id）；按需懒创建，容器挂在管理器下。</summary>
        static ObjectPoolMgr s_Pools;

        // WHY: Inst* 管理器各自持有随管理器生命周期的独立池，混用这里的池会让池与包的生命周期错位
        /// <summary>全局共享池：适用于「没有 Inst* 管理器、但仍想复用实例」的场合（如弹层、列表项）</summary>
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

        // WHY: 这是本工程唯一的预制体加载入口，直接 Resources.Load / AssetBundle.LoadAsset 会绕过实例登记与卸载
        /// <summary>从包配置 id 异步加载预制体（走 GlobalAddressableMgr 三链路 + 资源缓存）</summary>
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

        // WHY: maxIdleCount<=0 表示不限闲置数，preload 只在首次建池时生效（池已存在则忽略）
        /// <summary>加载并池化取用：池已就绪则同步返回，否则先加载预制体再建池取用</summary>
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

            // WHY: 池已建好就走同步热路径，避免额外加载与 GC
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
            var pools = s_Pools;                     // WHY: 不主动创建池（归还不应触发建池/加载）
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

                    // WHY: 并发加载同一图片时 CacheImage 会销毁重复的，必须用返回的生效实例
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
        /// <summary>构造 StreamingAssets 完整路径（WebGL 兼容）</summary>
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
