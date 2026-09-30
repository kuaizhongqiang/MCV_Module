using System;
using System.Collections;
using System.Collections.Generic;
using MCV_Module.InputController;
using MCV_Module.Singleton;
using MCV_Module.Utils;
using MCV_Module.Utils.Pool;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MCV_Module.Managers.InstManagers
{
    /// <summary>实例管理器基类：按包配置 id 加载预制体，再经对象池取用/归还（InstShowManager、InspectionManager 继承它）。</summary>
    public abstract class InstManagerBase : SingletonBase
    {
        #region 序列化参数
        [SerializeField] InputControllerBase mainCamController;
        [Header("包配置 id（PackageConfigSO.id，一个 id 对应一个预制体）")]
        [SerializeField] string[] packageKeys = new string[0];

        [Header("活跃实例父节点")]
        [Tooltip("取用出的实例挂在这里；为空时兜底用 1_Content 场景的 InstRoot 容器")]
        [SerializeField] protected Transform objParent;

        [Header("对象池参数")]
        [Tooltip("每个包预热实例数（0 = 只加载预制体，不预热实例）")]
        [SerializeField] int preloadCount = 0;

        [Tooltip("每个包闲置实例上限；归还时超出即销毁（<=0 表示不限）")]
        [SerializeField] int maxIdleCount = 24;

        [Tooltip("等待依赖就绪（GlobalAddressableMgr / 预制体加载回调）的最长秒数")]
        [SerializeField] float maxWaitTime = 5f;

        [Tooltip("勾选后把闲置实例容器搬到 1_Content 场景根下，随该场景卸载一起销毁")]
        [SerializeField] bool movePoolToInstScene = false;
        #endregion

        #region 常量
        /// <summary>实例场景名（活跃/闲置实例都归属它，卸载该场景时一并销毁）。</summary>
        const string InstSceneName = "1_Content";

        /// <summary>实例根容器名（1_Content 场景根下的容器）。</summary>
        const string InstRootName = "InstRoot";

        /// <summary>池闲置容器名（挂在活跃父节点下）。</summary>
        const string PoolRootName = "__InstPool";
        #endregion

        #region 状态
        /// <summary>是否可操控型（由子类决定：仅展示 = false，可操控 = true）。</summary>
        protected bool isControlled = false;

        /// <summary>是否允许 packageKeys 留空：true = 按需加载型（子类决定），空配置不预加载也不报错。</summary>
        protected virtual bool AllowEmptyPackageKeys => false;

        /// <summary>全部包的预制体是否已加载完成（Spawn 的同步前提）。</summary>
        protected bool isAllPackagesLoaded = false;

        /// <summary>包配置 id → 已加载的预制体（本管理器的加载缓存）。</summary>
        protected readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        /// <summary>本管理器的对象池注册表（key = 包配置 id）；用 <see cref="Pool"/> 访问更省心。</summary>
        protected ObjectPoolMgr pool;

        /// <summary>闲置实例容器。</summary>
        Transform poolRoot;

        /// <summary>是否正在加载包（防止 DelayInit 与 InitManager 重复触发）。</summary>
        bool isLoadingPackages;
        #endregion

        #region 对外属性
        /// <summary>全部包的预制体是否已就绪。</summary>
        public bool IsReady => isAllPackagesLoaded;

        /// <summary>受管的包配置 id 数量。</summary>
        public int PackageCount => packageKeys != null ? packageKeys.Length : 0;

        /// <summary>对象池注册表（首次访问时兜底创建）。</summary>
        public ObjectPoolMgr Pool
        {
            get { return EnsurePool(); }
        }
        #endregion

        #region 生命周期
        /// <summary>只做池容器就位；子类先绑自己的静态单例，再调 base.Awake()。</summary>
        protected virtual void Awake()
        {
            EnsurePool();
        }

        protected override IEnumerator DelayInit()
        {
            isAllPackagesLoaded = false;

            float time = 0f;
            while (packageKeys == null || packageKeys.Length == 0)
            {
                if (AllowEmptyPackageKeys)
                {
                    // 按需加载型：空配置是合法状态，不预加载也不报错，池在首次 SpawnAsync 时按需建立
                    Log.Verbose($"[{GetType().Name}] 未配置 packageKeys（按需加载型），包与对象池将在首次取用时建立");
                    isInit = true;
                    yield break;
                }

                time += Time.deltaTime;
                if (time > maxWaitTime)
                {
                    Log.Error($"[{GetType().Name}] packageKeys 为空，实例池未建立" +
                              "（请在 Inspector 配置，或运行时调 InitManager 注入）");
                    isInit = true;
                    yield break;
                }
                yield return null;
            }

            yield return RunLoad(packageKeys);

            if (movePoolToInstScene) SetSceneRoot();
            isInit = true;
        }

        protected virtual void OnDestroy()
        {
            // 管理器随场景销毁：把池内实例一并销毁，避免容器被回收后残留空引用
            if (pool != null)
            {
                pool.Dispose(true);
                pool = null;
            }
            prefabCache.Clear();
        }
        #endregion

        #region 包加载（走标准包管线）
        /// <summary>加载全部包预制体并建池；单个包失败只记日志跳过，不中断其余包。</summary>
        protected virtual IEnumerator LoadPackageAsync(string[] keys, Action<bool> onComplete)
        {
            // Inst* 不在 Setup 的启动链上，不能假设 GlobalAddressableMgr 已初始化 → 自行等待
            float wait = 0f;
            while (!(GlobalAddressableMgr.Exists && GlobalAddressableMgr.Instance != null && GlobalAddressableMgr.Instance.IsInit))
            {
                wait += Time.deltaTime;
                if (wait > maxWaitTime)
                {
                    Log.Error($"[{GetType().Name}] 等待 GlobalAddressableMgr 就绪超时（{maxWaitTime}s），包加载中止");
                    onComplete?.Invoke(false);
                    yield break;
                }
                yield return null;
            }

            int loaded = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                string id = keys[i];
                if (string.IsNullOrEmpty(id)) continue;

                if (prefabCache.ContainsKey(id))
                {
                    loaded++;
                    continue;
                }

                GameObject prefab = null;
                yield return LoadPrefabRoutine(id, p => prefab = p);
                if (prefab == null) continue;      // 失败原因已在 LoadPrefabRoutine 里记过

                prefabCache[id] = prefab;
                EnsurePool().CreatePool(id, prefab, maxIdleCount, preloadCount, CreateTrackedInstance);
                loaded++;
            }

            Log.Info($"[{GetType().Name}] 包加载完成：{loaded}/{keys.Length}（对象池就绪，可同步 Spawn）");
            onComplete?.Invoke(loaded > 0);
        }

        /// <summary>把异步回调式加载包成协程（回调可能同步触发，用 done 标记保护）。</summary>
        IEnumerator LoadPrefabRoutine(string packageId, Action<GameObject> onLoaded)
        {
            bool done = false;
            GameObject result = null;

            GlobalAssetsMgr.LoadPrefabAsync(packageId,
                prefab => { result = prefab; done = true; },
                error => { Log.Error($"[{GetType().Name}] {error}"); done = true; });

            float time = 0f;
            while (!done && time < maxWaitTime)
            {
                time += Time.deltaTime;
                yield return null;
            }

            if (!done) Log.Error($"[{GetType().Name}] 预制体加载超时：{packageId}");
            onLoaded?.Invoke(result);
        }

        /// <summary>池内实例的创建工厂：经 GlobalAddressableMgr 创建并登记包归属（卸包时可一并销毁）。</summary>
        GameObject CreateTrackedInstance(string key, GameObject prefab, Transform parent)
        {
            if (GlobalAddressableMgr.Exists && GlobalAddressableMgr.Instance != null)
                return GlobalAddressableMgr.Instance.InstantiatePrefab(prefab, key, parent);

            return UnityEngine.Object.Instantiate(prefab, parent);
        }

        IEnumerator RunLoad(string[] keys)
        {
            if (isLoadingPackages) yield break;

            isLoadingPackages = true;
            try
            {
                yield return LoadPackageAsync(keys, ok => isAllPackagesLoaded = ok);
            }
            finally
            {
                isLoadingPackages = false;
            }
        }
        #endregion

        #region 公开方法 —— 配置
        /// <summary>注入包配置 id 与受控类型（可替代 Inspector 配置，运行时调用会补一次加载）。</summary>
        public void InitManager(string[] keys, bool isControlled)
        {
            SetPackageKeys(keys);
            SetControlled(isControlled);

            if (isActiveAndEnabled && !isAllPackagesLoaded) StartCoroutine(RunLoad(packageKeys));
        }

        /// <summary>注入包配置 id（保持原有受控类型）。</summary>
        public void InitManager(string[] keys)
        {
            InitManager(keys, isControlled);
        }

        /// <summary>注入活跃实例父节点；已创建的池容器会跟着搬过去。</summary>
        public void SetParent(Transform parent)
        {
            objParent = parent;
            if (poolRoot != null && parent != null) poolRoot.SetParent(parent, false);
        }
        #endregion

        #region 公开方法 —— 取用 / 归还
        /// <summary>从池中同步取一个实例（未预加载的包返回 null，请改用 <see cref="SpawnAsync"/>）。</summary>
        public GameObject Spawn(string packageId, Transform parent = null)
        {
            return Spawn(packageId, parent, Vector3.zero, Quaternion.identity);
        }

        /// <summary>从池中同步取一个实例并摆位。</summary>
        public GameObject Spawn(string packageId, Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                Log.Error($"[{GetType().Name}] Spawn 失败：packageId 为空");
                return null;
            }

            var pools = EnsurePool();
            if (pools.Get(packageId) == null && !CreatePoolFromCache(packageId)) return null;

            var instance = pools.Spawn(packageId, parent != null ? parent : ResolveSpawnParent(), localPosition, localRotation);
            if (instance != null) Log.Verbose($"[{GetType().Name}] Spawn {packageId} → {instance.name}");
            return instance;
        }

        /// <summary>同步取用并取组件 <typeparamref name="T"/>（取不到时归还并返回 null）。</summary>
        public T Spawn<T>(string packageId, Transform parent = null) where T : Component
        {
            return Spawn<T>(packageId, parent, Vector3.zero, Quaternion.identity);
        }

        /// <summary>同步取用并取组件 <typeparamref name="T"/>（取不到时归还并返回 null）。</summary>
        public T Spawn<T>(string packageId, Transform parent, Vector3 localPosition, Quaternion localRotation) where T : Component
        {
            var instance = Spawn(packageId, parent, localPosition, localRotation);
            if (instance == null) return null;

            var component = instance.GetComponentInChildren<T>();
            if (component == null)
            {
                Log.Error($"[{GetType().Name}] {packageId} 上找不到组件 {typeof(T).Name}，实例已归还");
                Despawn(instance);
            }
            return component;
        }

        /// <summary>异步取用：已建池则同步回调，否则先加载建池再取用（用于运行时才知道包 id 的场合）。</summary>
        public void SpawnAsync(string packageId, Transform parent, Action<GameObject> onSpawned, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                onError?.Invoke($"[{GetType().Name}] packageId 为空");
                return;
            }

            var pools = EnsurePool();
            if (pools.Contains(packageId))
            {
                onSpawned?.Invoke(pools.Spawn(packageId, parent != null ? parent : ResolveSpawnParent()));
                return;
            }

            GlobalAssetsMgr.LoadPrefabAsync(packageId, prefab =>
            {
                prefabCache[packageId] = prefab;
                pools.CreatePool(packageId, prefab, maxIdleCount, preloadCount, CreateTrackedInstance);
                onSpawned?.Invoke(pools.Spawn(packageId, parent != null ? parent : ResolveSpawnParent()));
            }, onError);
        }

        /// <summary>归还实例到池（非本管理器池内对象返回 false）。</summary>
        public bool Despawn(GameObject instance)
        {
            if (instance == null) return false;

            var pools = pool;                       // 不主动建池
            if (pools == null || !pools.Despawn(instance))
            {
                Log.Warning($"[{GetType().Name}] Despawn 失败：{instance.name} 不属于本管理器的对象池");
                return false;
            }
            return true;
        }

        /// <summary>归还全部活跃实例（保留池与已加载的预制体），返回归还数量。</summary>
        public int DespawnAll()
        {
            return pool != null ? pool.DespawnAll() : 0;
        }

        /// <summary>归还指定包的全部活跃实例。</summary>
        public int DespawnAll(string packageId)
        {
            return pool != null ? pool.DespawnAll(packageId) : 0;
        }
        #endregion

        #region 公开方法 —— 预热 / 释放
        /// <summary>预热指定包的实例（包未预加载但已有缓存预制体时也可以）；返回新建数量。</summary>
        public int Preload(string packageId, int count)
        {
            if (count <= 0 || string.IsNullOrEmpty(packageId)) return 0;

            var pools = EnsurePool();
            var target = pools.Get(packageId);
            if (target == null)
            {
                if (!CreatePoolFromCache(packageId)) return 0;
                target = pools.Get(packageId);
            }
            return target != null ? target.Preload(count) : 0;
        }

        /// <summary>释放单个包：销毁其池（含闲置/活跃实例）并清掉预制体缓存。</summary>
        public void ReleasePackage(string packageId, bool destroyInstances = true)
        {
            if (string.IsNullOrEmpty(packageId)) return;

            prefabCache.Remove(packageId);
            if (pool != null) pool.DestroyPool(packageId, destroyInstances);
        }

        /// <summary>释放全部包（管理器销毁/切场景收口）。</summary>
        public void ReleaseAllPackages(bool destroyInstances = true)
        {
            prefabCache.Clear();
            if (pool != null) pool.DestroyAllPools(destroyInstances);
        }
        #endregion

        #region 场景根与池容器
        /// <summary>取 1_Content 场景根下的 InstRoot 容器（场景未加载返回 null，容器不存在则创建）。</summary>
        protected Transform GetSceneRoot()
        {
            var scene = SceneManager.GetSceneByName(InstSceneName);
            if (!scene.IsValid() || !scene.isLoaded) return null;

            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == InstRootName) return roots[i].transform;
            }

            var go = new GameObject(InstRootName);
            SceneManager.MoveGameObjectToScene(go, scene);      // 新建对象默认落在激活场景，这里显式搬过去
            return go.transform;
        }

        /// <summary>把闲置实例容器搬到实例场景根下，让池内对象随 1_Content 卸载一起销毁。</summary>
        protected void SetSceneRoot()
        {
            var root = GetSceneRoot();
            if (root == null)
            {
                Log.Warning($"[{GetType().Name}] 场景 {InstSceneName} 未加载，池容器仍挂在 objParent 下");
                return;
            }
            if (poolRoot != null && poolRoot.parent != root) poolRoot.SetParent(root, false);
        }

        /// <summary>活跃实例的默认父节点：优先 objParent，其次池容器。</summary>
        Transform ResolveSpawnParent()
        {
            if (objParent != null) return objParent;
            return EnsurePoolRoot();
        }

        ObjectPoolMgr EnsurePool()
        {
            if (pool == null) pool = new ObjectPoolMgr(EnsurePoolRoot());
            return pool;
        }

        Transform EnsurePoolRoot()
        {
            if (poolRoot != null) return poolRoot;

            Transform parent = objParent != null ? objParent : GetSceneRoot();
            if (parent == null) parent = transform;    // 兜底：挂管理器自身下，保证池容器有明确归属

            var go = new GameObject(PoolRootName);
            go.transform.SetParent(parent, false);
            poolRoot = go.transform;
            return poolRoot;
        }

        /// <summary>用已缓存的预制体建池；没有缓存则记错误并返回 false。</summary>
        bool CreatePoolFromCache(string packageId)
        {
            if (!prefabCache.TryGetValue(packageId, out var prefab) || prefab == null)
            {
                Log.Error($"[{GetType().Name}] 包 {packageId} 尚未加载" +
                          "（等 IsReady 为 true，或改用 SpawnAsync / InitManager 注入该包）");
                return false;
            }

            EnsurePool().CreatePool(packageId, prefab, maxIdleCount, 0, CreateTrackedInstance);
            return true;
        }
        #endregion

        #region 私有方法
        void SetPackageKeys(string[] keys)
        {
            packageKeys = keys ?? new string[0];
        }

        void SetControlled(bool isControlled)
        {
            this.isControlled = isControlled;
        }
        #endregion

        #region 输入控制的切换
        bool HasMainRegisted()
        {
            var controller = GlobalInputMgr.GetController<InputControllerBase>();
            return controller != null ;
        }

        protected void SetMainControllerActive(bool isActive)
        {
            if (HasMainRegisted())
            {
                mainCamController.IsActive = isActive;
            }
            else
            {
                Log.Warning($"[{GetType().Name}] 未注册主控制器");
            }
        } 
        #endregion
    }
}
