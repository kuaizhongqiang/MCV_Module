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
            // P5 修复既有缺陷：原实现完成构建后未置 isInit=true，导致 Setup 启动链每次等待 15s 超时
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
                Debug.LogError($"[AA] 场景 {sceneName} 未找到 AA 配置");
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
                Debug.LogError($"[AA] 场景 {sceneName} 的 sceneAsset 未赋值");
            }
#else
            Log.Verbose($"[AA] Runtime 模式: Addressables.LoadSceneAsync({address})");
            var handle = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(address, mode);
            yield return handle;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                m_SceneHandles[sceneName] = handle; // 保留 handle，便于卸载时释放包
                Log.Verbose($"[AA] AA 场景加载成功: {sceneName}, address={address}");
            }
            else
            {
                Debug.LogError($"[AA] AA 场景加载失败: {sceneName}, address={address}, error={handle.OperationException}");
            }
#endif
        }

        /// <summary>
        /// 卸载场景并释放其 AA 包（运行时经 SceneInstance handle；编辑器模式走 SceneManager）。
        /// 供 GlobalSceneMgr 交换场景时调用。
        /// </summary>
        public IEnumerator UnloadSceneAsync(string sceneName)
        {
#if UNITY_EDITOR
            // 编辑器模式：场景经 EditorSceneManager 加载，无 AA handle，直接用 SceneManager 卸载
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

        // ── 统一加载入口 ──────────────────────────────────────

        public void LoadAssetAsync<T>(string packageId, System.Action<T> onLoaded) where T : Object
        {
            if (!m_ConfigMap.TryGetValue(packageId, out var config))
            {
                Debug.LogError($"[AddrMgr] 未找到包配置: {packageId}");
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

            if (m_AssetCache.TryGetValue(config.id, out var cached) && cached is T)
            {
                onLoaded?.Invoke(cached as T);
                return;
            }

            switch (config.PackageType)
            {
                case PackageType.AA:
                    StartCoroutine(LoadFromAA(config.id, config.GetLoadKey(), onLoaded));
                    break;
                case PackageType.AB:
                    StartCoroutine(LoadFromAB(config.id, (ABPackageConfigSO)config, onLoaded));
                    break;
                case PackageType.Default:
                    LoadFromDefault(config.id, config.GetLoadKey(), onLoaded);
                    break;
            }
        }

        // ── 卸载与缓存管理 ────────────────────────────────────

        public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (m_BundleCache.TryGetValue(bundleName, out var bundle))
            {
                bundle.Unload(unloadAllLoadedObjects);
                m_BundleCache.Remove(bundleName);
            }
        }

        public void UnloadAllBundles(bool unloadAllLoadedObjects = false)
        {
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
                Debug.LogError($"[AddrMgr] AA 加载失败: {address}, {handle.OperationException}");
                onLoaded?.Invoke(null);
            }
        }

        private IEnumerator LoadFromAB<T>(string cacheKey, ABPackageConfigSO config, System.Action<T> onLoaded) where T : Object
        {
            if (string.IsNullOrEmpty(config.bundleName))
            {
                Debug.LogError($"[AddrMgr] AB 配置缺少 bundleName: {config.id}");
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
                    Debug.LogError($"[AddrMgr] AB 加载失败: {config.bundleName}, {uwr.error}");
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
        #endregion

        #region 工具方法
        private static string GetBundleUrl(string bundleName)
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, bundleName);
#if UNITY_WEBGL && !UNITY_EDITOR
            return path;
#else
            return "file://" + path;
#endif
        }
        #endregion
    }
}
