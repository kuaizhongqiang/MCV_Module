using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using MCV_Module.Singleton;
using MCV_Module.Models.Addressable;
using MCV_Module.Utils;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MCV_Module.Managers
{
    /// <summary>包配置注册表 + AA/AB/Default 三条资源加载链路 + 实例化与包归属登记。</summary>
    public class GlobalAddressableMgr : SingletonGlobalMgr<GlobalAddressableMgr>
    {
        #region 参数
        [Header("包配置数据库")]
        [SerializeField] private List<PackageDatabaseSO> m_PackageDatabases = new();

        [Header("场景 AA 配置")]
        [Tooltip("场景 AA 配置表，定义哪些场景走 Addressables")]
        [SerializeField] private SceneAddressableConfig m_SceneConfig;

        private readonly Dictionary<string, PackageConfigSO> m_ConfigMap = new();
        private readonly Dictionary<string, AssetBundle> m_BundleCache = new();
        private readonly Dictionary<string, Object> m_AssetCache = new();

        /// <summary>经本管理器实例化的存活对象 → 包配置 id（卸载资源包时据此一并销毁实例）</summary>
        private readonly Dictionary<GameObject, string> m_InstanceMap = new();

        /// <summary>场景 handle 缓存：sceneName → SceneInstance handle（运行时 AA 加载时写入，用于卸载释放包）</summary>
        private readonly Dictionary<string, UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>> m_SceneHandles = new();

        /// <summary>场景 address → SceneAAEntry 映射</summary>
        private readonly Dictionary<string, SceneAAEntry> m_SceneMap = new();

        /// <summary>场景名 → address 反向映射（O(1) 查询，避免每次遍历）</summary>
        private readonly Dictionary<string, string> m_SceneNameToAddress = new();
        #endregion

        #region 生命周期
        protected GlobalAddressableMgr() { }

        protected override IEnumerator DelayInit()
        {
            BuildConfigMap();
            BuildSceneMap();
            Log.Verbose($"[AddrMgr] 字典构建完成，包: {m_ConfigMap.Count}，AA 场景: {m_SceneMap.Count}");
            // WHY: 字典构建完必须置 isInit=true，漏置会让 Setup 启动链每次都白等 15s 超时
            isInit = true;
            yield break;
        }
        #endregion

        #region 公开方法
        // ── 场景 AA 查询 ───────────────────────────────────────

        /// <summary>判断场景是否配置为 AA 加载（O(1) 反向索引）</summary>
        public bool IsSceneAA(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) && m_SceneNameToAddress.ContainsKey(sceneName);
        }

        /// <summary>获取场景的 AA address（O(1) 反向索引）</summary>
        public string GetSceneAddress(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) && m_SceneNameToAddress.TryGetValue(sceneName, out var address)
                ? address
                : null;
        }

        // ── 场景加载（提供给 GlobalSceneMgr 调用） ──────────────

        /// <summary>通过 AA 异步加载场景</summary>
        public IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode)
        {
            string address = GetSceneAddress(sceneName);
            Log.Verbose($"[AA] LoadSceneAsync: scene={sceneName}, address={address}");

            if (string.IsNullOrEmpty(address))
            {
                Log.Error($"[AA] 场景 {sceneName} 未找到 AA 配置");
                yield break;
            }

            yield return null;

#if UNITY_EDITOR
            Log.Verbose($"[AA] Editor 模式: 用 EditorSceneManager 加载 {sceneName}");
            var entry = m_SceneMap[address];
            if (entry.sceneAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(entry.sceneAsset);
                Log.Verbose($"[AA] 场景路径: {path}");
                var op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    path, new LoadSceneParameters(mode));
                yield return op;
                Log.Verbose($"[AA] Editor 场景加载完成: {sceneName}");
            }
            else
            {
                Log.Error($"[AA] 场景 {sceneName} 的 sceneAsset 未赋值");
            }
#else
            Log.Verbose($"[AA] Runtime 模式: Addressables.LoadSceneAsync({address})");
            var handle = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(address, mode);
            yield return handle;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                m_SceneHandles[sceneName] = handle; // WHY: handle 必须留到卸载时用，丢了就永远释放不了该场景的 AA 包
                Log.Verbose($"[AA] AA 场景加载成功: {sceneName}, address={address}");
            }
            else
            {
                Log.Error($"[AA] AA 场景加载失败: {sceneName}, address={address}, error={handle.OperationException}");
            }
#endif
        }

        /// <summary>卸载场景并释放其 AA 包（Editor 走 SceneManager，运行时走 handle）</summary>
        public IEnumerator UnloadSceneAsync(string sceneName)
        {
#if UNITY_EDITOR
            // WHY: Editor 下场景经 EditorSceneManager 加载、没有 AA handle，只能走 SceneManager 卸载
            yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
#else
            if (m_SceneHandles.TryGetValue(sceneName, out var handle))
            {
                var op = UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(handle, true);
                yield return op;
                m_SceneHandles.Remove(sceneName);
                Log.Verbose($"[AA] AA 场景已卸载并释放: {sceneName}");
            }
            else
            {
                yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
            }
#endif
        }

        // ── 包查询 ─────────────────────────────────────────────

        public PackageConfigSO GetConfig(string id)
        {
            m_ConfigMap.TryGetValue(id, out var config);
            return config;
        }

        public T GetConfig<T>(string id) where T : PackageConfigSO
        {
            return m_ConfigMap.TryGetValue(id, out var config) && config is T typed ? typed : null;
        }

        public bool TryGetConfig(string id, out PackageConfigSO config)
        {
            return m_ConfigMap.TryGetValue(id, out config);
        }

        public IEnumerable<PackageConfigSO> GetAllConfigs(PackageType type)
        {
            foreach (var kvp in m_ConfigMap)
            {
                if (kvp.Value.PackageType == type)
                    yield return kvp.Value;
            }
        }

        public int ConfigCount => m_ConfigMap.Count;

        // ── 内容包（一个 ProjectClip 一个 bundle，按 clipId 聚合） ──

        /// <summary>取 clipId 相同的全部 AB 配置，供 GlobalAssetsMgr 一个 clip 一次装卸</summary>
        public List<ABPackageConfigSO> GetConfigsByClip(string clipId)
        {
            var list = new List<ABPackageConfigSO>();
            if (string.IsNullOrEmpty(clipId)) return list;

            foreach (var kvp in m_ConfigMap)
            {
                if (kvp.Value is ABPackageConfigSO ab && ab.clipId == clipId) list.Add(ab);
            }
            return list;
        }

        // WHY: 全局包没有 clipId，GetConfigsByClip 收集不到它；按 bundleName 收集是全局包唯一的聚合口径（卸载侧的 UnloadByBundleName 与它同口径）。
        /// <summary>取某个 bundle 名下的全部 AB 配置（全局包预加载用；clipId 为空的包靠它聚合）</summary>
        public List<ABPackageConfigSO> GetConfigsByBundleName(string bundleName)
        {
            var list = new List<ABPackageConfigSO>();
            if (string.IsNullOrEmpty(bundleName)) return list;

            foreach (var kvp in m_ConfigMap)
            {
                if (kvp.Value is ABPackageConfigSO ab && ab.bundleName == bundleName) list.Add(ab);
            }
            return list;
        }

        /// <summary>同步读缓存（不触发加载）；未加载/类型不符/已销毁都返回 false</summary>
        public bool TryGetCached<T>(string packageId, out T asset) where T : Object
        {
            asset = null;
            if (string.IsNullOrEmpty(packageId)) return false;
            if (!m_AssetCache.TryGetValue(packageId, out var cached)) return false;

            asset = cached as T;
            return asset != null;
        }

        // WHY: 缓存必须先清再卸 bundle —— LoadAssetRoutine 命中缓存时不判已销毁，残留条目会让下一次取用拿到 destroyed 对象（图片空白）
        /// <summary>卸载一个 clip：先失效资源缓存再卸 bundle（连带销毁该包实例），返回失效条数</summary>
        public int UnloadClip(string clipId)
        {
            List<ABPackageConfigSO> configs = GetConfigsByClip(clipId);
            if (configs.Count == 0) return 0;

            var bundles = new HashSet<string>();
            int invalidated = 0;

            foreach (ABPackageConfigSO config in configs)
            {
                if (m_AssetCache.Remove(config.id)) invalidated++;
                if (!string.IsNullOrEmpty(config.bundleName)) bundles.Add(config.bundleName);
            }

            foreach (string bundle in bundles) UnloadBundle(bundle, true);

            Log.Info($"[AddrMgr] 已卸载内容包 {clipId}：失效缓存 {invalidated} 条 / bundle {bundles.Count} 个");
            return invalidated;
        }

        // WHY: 缓存必须先清再卸，理由同 UnloadClip；空名直接返回 0，防止空串误命中别的包
        /// <summary>按 bundle 名卸载（全局包专用，clipId 为空走不了 UnloadClip；先清缓存再卸）；空名返回 0</summary>
        public int UnloadByBundleName(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName)) return 0;

            int invalidated = 0;
            foreach (var kvp in m_ConfigMap)
            {
                if (kvp.Value is ABPackageConfigSO ab && ab.bundleName == bundleName)
                {
                    if (m_AssetCache.Remove(ab.id)) invalidated++;
                }
            }

            UnloadBundle(bundleName, true);

            Log.Info($"[AddrMgr] 已卸载全局包 {bundleName}：失效缓存 {invalidated} 条");
            return invalidated;
        }

        /// <summary>清单里出现过的全部 clipId（诊断用：找不到某 clip 的配置时，一眼看出字典里实际有谁）。</summary>
        public List<string> GetClipIds()
        {
            var set = new HashSet<string>();
            foreach (var kvp in m_ConfigMap)
            {
                if (kvp.Value is ABPackageConfigSO ab && !string.IsNullOrEmpty(ab.clipId)) set.Add(ab.clipId);
            }
            return new List<string>(set);
        }

        // ── 统一加载入口 ──────────────────────────────────────

        public void LoadAssetAsync<T>(string packageId, System.Action<T> onLoaded) where T : Object
        {
            if (!m_ConfigMap.TryGetValue(packageId, out var config))
            {
                Log.Error($"[AddrMgr] 未找到包配置: {packageId}");
                onLoaded?.Invoke(null);
                return;
            }
            LoadAssetAsync(config, onLoaded);
        }

        public void LoadAssetAsync<T>(PackageConfigSO config, System.Action<T> onLoaded) where T : Object
        {
            if (config == null)
            {
                onLoaded?.Invoke(null);
                return;
            }
            StartCoroutine(LoadAssetRoutine(config, onLoaded));
        }

        /// <summary>按包配置 id 保序批量加载；单项失败只记日志跳过，不中断其余项</summary>
        public void LoadAssetsAsync<T>(IList<string> packageIds, System.Action<List<T>> onLoaded) where T : Object
        {
            if (packageIds == null || packageIds.Count == 0)
            {
                onLoaded?.Invoke(new List<T>());
                return;
            }
            StartCoroutine(LoadAssetsRoutine(packageIds, onLoaded));
        }

        IEnumerator LoadAssetsRoutine<T>(IList<string> packageIds, System.Action<List<T>> onLoaded) where T : Object
        {
            var loaded = new List<T>(packageIds.Count);
            for (int i = 0; i < packageIds.Count; i++)
            {
                string id = packageIds[i];
                if (string.IsNullOrEmpty(id)) continue;

                if (!m_ConfigMap.TryGetValue(id, out var config))
                {
                    Log.Error($"[AddrMgr] 未找到包配置: {id}（检查 PackageDB_Master 是否收录该配置）");
                    continue;
                }

                yield return LoadAssetRoutine<T>(config, asset =>
                {
                    if (asset != null) loaded.Add(asset);
                });
            }
            onLoaded?.Invoke(loaded);
        }

        /// <summary>单个资源加载（可被批量加载以协程嵌套复用）：命中缓存直接回调，未命中按 PackageType 走对应链路。</summary>
        IEnumerator LoadAssetRoutine<T>(PackageConfigSO config, System.Action<T> onLoaded) where T : Object
        {
            if (m_AssetCache.TryGetValue(config.id, out var cached) && cached is T)
            {
                onLoaded?.Invoke(cached as T);
                yield break;
            }

            switch (config.PackageType)
            {
                case PackageType.AA:
                    yield return LoadFromAA<T>(config.id, config.GetLoadKey(), onLoaded);
                    break;
                case PackageType.AB:
                    yield return LoadFromAB<T>(config.id, (ABPackageConfigSO)config, onLoaded);
                    break;
                default:
                    LoadFromDefault<T>(config.id, config.GetLoadKey(), onLoaded);
                    break;
            }
        }

        // ── 实例化（预制体 → GameObject，与资源加载同一条包管线） ──
        // WHY: 只用 Object.Instantiate，不用 Addressables.InstantiateAsync —— 生命周期归包管、对象池要能自由 Destroy 复用、三链路口径一致

        /// <summary>已登记的存活实例数（顺带清理被外部销毁的空槽）。</summary>
        public int InstanceCount
        {
            get { PruneInstances(); return m_InstanceMap.Count; }
        }

        /// <summary>由已加载的预制体实例化并登记包归属（对象池实例工厂用，预制体已有缓存不再走异步）</summary>
        public GameObject InstantiatePrefab(GameObject prefab, string packageId, Transform parent = null)
        {
            if (prefab == null)
            {
                Log.Error($"[AddrMgr] 实例化失败：预制体为空（包 {packageId}）");
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            if (instance == null) return null;

            instance.name = prefab.name;               // 去掉 Instantiate 追加的 "(Clone)"
            m_InstanceMap[instance] = packageId;
            return instance;
        }

        /// <summary>按包配置 id 异步加载预制体并实例化（AA / AB / Default 三链路）；失败回调 null。</summary>
        public void InstantiateAsync(string packageId, Transform parent, System.Action<GameObject> onComplete)
        {
            if (!m_ConfigMap.TryGetValue(packageId, out var config))
            {
                Log.Error($"[AddrMgr] 实例化失败：未找到包配置 {packageId}");
                onComplete?.Invoke(null);
                return;
            }
            InstantiateAsync(config, parent, onComplete);
        }

        /// <summary>按包配置实例化（同上，直接给配置对象）。</summary>
        public void InstantiateAsync(PackageConfigSO config, Transform parent, System.Action<GameObject> onComplete)
        {
            if (config == null)
            {
                onComplete?.Invoke(null);
                return;
            }
            StartCoroutine(InstantiateRoutine(config, parent, onComplete));
        }

        IEnumerator InstantiateRoutine(PackageConfigSO config, Transform parent, System.Action<GameObject> onComplete)
        {
            GameObject prefab = null;
            yield return LoadAssetRoutine<GameObject>(config, loaded => prefab = loaded);
            onComplete?.Invoke(prefab != null ? InstantiatePrefab(prefab, config.id, parent) : null);
        }

        /// <summary>销毁经本管理器实例化的对象并注销登记（池化实例请走池归还接口）</summary>
        public bool ReleaseInstance(GameObject instance, bool destroy = true)
        {
            if (instance == null) return false;

            m_InstanceMap.Remove(instance);
            if (destroy) UnityEngine.Object.Destroy(instance);
            return true;
        }

        /// <summary>销毁某包的全部存活实例，返回销毁数量（卸载包前后调用，避免实例引用已释放的资源）。</summary>
        public int DestroyInstances(string packageId)
        {
            if (string.IsNullOrEmpty(packageId) || m_InstanceMap.Count == 0) return 0;

            var targets = new List<GameObject>();
            foreach (var kvp in m_InstanceMap)
            {
                if (kvp.Key != null && kvp.Value == packageId) targets.Add(kvp.Key);
            }

            for (int i = 0; i < targets.Count; i++)
            {
                m_InstanceMap.Remove(targets[i]);
                UnityEngine.Object.Destroy(targets[i]);
            }
            return targets.Count;
        }

        /// <summary>销毁全部登记的存活实例，返回销毁数量。</summary>
        public int DestroyAllInstances()
        {
            if (m_InstanceMap.Count == 0) return 0;

            int count = 0;
            foreach (var kvp in m_InstanceMap)
            {
                if (kvp.Key == null) continue;
                UnityEngine.Object.Destroy(kvp.Key);
                count++;
            }
            m_InstanceMap.Clear();
            return count;
        }

        // ── 卸载与缓存管理 ────────────────────────────────────

        /// <summary>卸载单个 AssetBundle；unloadAllLoadedObjects=true 时先销毁该包存活实例（防变粉）</summary>
        public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (unloadAllLoadedObjects)
            {
                foreach (var kvp in m_ConfigMap)
                {
                    if (kvp.Value is ABPackageConfigSO ab && ab.bundleName == bundleName)
                        DestroyInstances(kvp.Key);
                }
            }

            if (m_BundleCache.TryGetValue(bundleName, out var bundle))
            {
                bundle.Unload(unloadAllLoadedObjects);
                m_BundleCache.Remove(bundleName);
            }
        }

        public void UnloadAllBundles(bool unloadAllLoadedObjects = false)
        {
            if (unloadAllLoadedObjects) DestroyAllInstances();

            foreach (var kvp in m_BundleCache)
                kvp.Value.Unload(unloadAllLoadedObjects);
            m_BundleCache.Clear();
            m_AssetCache.Clear();
        }

        public void ClearAssetCache()
        {
            m_AssetCache.Clear();
        }

#if UNITY_EDITOR
        public T LoadInEditor<T>(string assetPath) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }
#endif
        #endregion

        #region 私有方法
        private void BuildConfigMap()
        {
            m_ConfigMap.Clear();

            // WHY: 清单未挂时兜底读 Resources/Config/PackageDB_Master，新增包配置不必再改 Manager.prefab
            if (m_PackageDatabases == null || m_PackageDatabases.Count == 0)
            {
                var db = Resources.Load<PackageDatabaseSO>("Config/PackageDB_Master");
                if (db != null)
                {
                    m_PackageDatabases = new List<PackageDatabaseSO> { db };
                    Log.Verbose("[AddrMgr] 包清单未挂载，已从 Resources/Config/PackageDB_Master 兜底加载");
                }
            }

            if (m_PackageDatabases == null) return;

            foreach (var db in m_PackageDatabases)
            {
                if (db == null) continue;
                foreach (var config in db.packages)
                {
                    if (config == null || string.IsNullOrEmpty(config.id)) continue;
                    m_ConfigMap[config.id] = config;
                }
            }
        }

        private void BuildSceneMap()
        {
            m_SceneMap.Clear();
            m_SceneNameToAddress.Clear();
            if (m_SceneConfig == null)
            {
                // 尝试从 Resources 加载
                m_SceneConfig = Resources.Load<SceneAddressableConfig>("Config/SceneAAConfig");
                if (m_SceneConfig == null) return;
            }

            foreach (var entry in m_SceneConfig.scenes)
            {
                if (string.IsNullOrEmpty(entry.address)) continue;
                m_SceneMap[entry.address] = entry;
                if (!string.IsNullOrEmpty(entry.sceneName))
                    m_SceneNameToAddress[entry.sceneName] = entry.address;
            }
        }

        private IEnumerator LoadFromAA<T>(string cacheKey, string address, System.Action<T> onLoaded) where T : Object
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(address);
            yield return handle;
            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                m_AssetCache[cacheKey] = handle.Result;
                onLoaded?.Invoke(handle.Result);
            }
            else
            {
                Log.Error($"[AddrMgr] AA 加载失败: {address}, {handle.OperationException}");
                onLoaded?.Invoke(null);
            }
        }

        private IEnumerator LoadFromAB<T>(string cacheKey, ABPackageConfigSO config, System.Action<T> onLoaded) where T : Object
        {
            if (string.IsNullOrEmpty(config.bundleName))
            {
                Log.Error($"[AddrMgr] AB 配置缺少 bundleName: {config.id}");
                onLoaded?.Invoke(null);
                yield break;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(config.assetPath);
                if (asset != null) m_AssetCache[cacheKey] = asset;
                onLoaded?.Invoke(asset);
                yield break;
            }
#endif

            if (!m_BundleCache.TryGetValue(config.bundleName, out var bundle))
            {
                var url = GetBundleUrl(config.bundleName);
                var uwr = UnityWebRequestAssetBundle.GetAssetBundle(url);
                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Log.Error($"[AddrMgr] AB 加载失败: {config.bundleName}, {uwr.error}");
                    onLoaded?.Invoke(null);
                    yield break;
                }
                bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                m_BundleCache[config.bundleName] = bundle;
            }

            var assetReq = bundle.LoadAssetAsync<T>(config.assetPath);
            yield return assetReq;
            var result = assetReq.asset as T;
            if (result != null) m_AssetCache[cacheKey] = result;
            onLoaded?.Invoke(result);
        }

        private void LoadFromDefault<T>(string cacheKey, string loadKey, System.Action<T> onLoaded) where T : Object
        {
            var asset = Resources.Load<T>(loadKey);
            if (asset != null) m_AssetCache[cacheKey] = asset;
            onLoaded?.Invoke(asset);
        }

        /// <summary>清理实例登记表中被外部销毁的空槽（Unity 的 == null 判空对已销毁对象成立）。</summary>
        private void PruneInstances()
        {
            if (m_InstanceMap.Count == 0) return;

            List<GameObject> dead = null;
            foreach (var kvp in m_InstanceMap)
            {
                if (kvp.Key == null)
                {
                    if (dead == null) dead = new List<GameObject>();
                    dead.Add(kvp.Key);
                }
            }
            if (dead == null) return;

            for (int i = 0; i < dead.Count; i++) m_InstanceMap.Remove(dead[i]);
        }
        #endregion

        #region 工具方法
        private static string GetBundleUrl(string bundleName)
        {
            // WHY: Unity 构建只小写 bundle 文件名、目录保持原样；不跟着小写会在 Linux/Android/WebGL 上找不到文件
            string normalized = bundleName;
            int lastSlash = normalized.LastIndexOf('/');
            normalized = lastSlash >= 0
                ? normalized.Substring(0, lastSlash + 1) + normalized.Substring(lastSlash + 1).ToLowerInvariant()
                : normalized.ToLowerInvariant();

            var path = System.IO.Path.Combine(Application.streamingAssetsPath, normalized);
#if UNITY_WEBGL && !UNITY_EDITOR
            // WHY: WebGL 下 StreamingAssets 就是 HTTP 路径，直接拼 URL，不能加 file:// 前缀
            return path.Replace("\\", "/");
#else
            // WHY: 本地必须用 Uri 转义成 file:// URL，否则工程路径含空格/中文时加载失败
            return new System.Uri(path).AbsoluteUri;
#endif
        }
        #endregion
    }
}
