using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using MCV_Module.Data.Addressable;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MCV_Module.GlobalManager
{
    /// <summary>
    /// AA / AB 包统一加载管理器
    ///
    /// 职责：
    ///   - 持有 PackageDatabaseSO 列表（Inspector 拖拽），启动时构建 id → PackageConfigSO 字典
    ///   - 统一加载入口：根据 PackageType 路由到 AA / AB / Default
    ///   - AB 包由 StreamingAssets 加载，Editor 下走 AssetDatabase 跳过打包
    ///   - 内置资产缓存和 AB 包生命周期管理
    /// </summary>
    public class GlobalAddressableMgr : SingletonGlobalMgr<GlobalAddressableMgr>
    {
        protected GlobalAddressableMgr() { }

        [Header("包配置数据库")]
        [Tooltip("将项目中创建的 PackageDatabaseSO 拖入此列表\n" +
                 "启动时会自动遍历所有数据库，构建内部字典供查询和加载")]
        [SerializeField] private List<PackageDatabaseSO> m_PackageDatabases = new();

        // ── 内部字典 ──────────────────────────────────────────
        /// <summary>id → PackageConfigSO 映射（由 m_PackageDatabases 构建）</summary>
        private readonly Dictionary<string, PackageConfigSO> m_ConfigMap = new();

        // ── 缓存 ──────────────────────────────────────────────
        /// <summary>已加载的 AB 包</summary>
        private readonly Dictionary<string, AssetBundle> m_BundleCache = new();

        /// <summary>已加载的资产（按 package id 缓存）</summary>
        private readonly Dictionary<string, Object> m_AssetCache = new();

        // ── 初始化 ────────────────────────────────────────────

        protected override IEnumerator DelayInit()
        {
            BuildConfigMap();
            Debug.Log($"[AddrMgr] 字典构建完成，共 {m_ConfigMap.Count} 个包配置");
            yield break;
        }

        /// <summary>
        /// 遍历 m_PackageDatabases，将每个数据库中的所有 PackageConfigSO
        /// 按 id 合并到 m_ConfigMap 中（重复 id 以后者为准）
        /// </summary>
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

        // ── 公共查询方法 ──────────────────────────────────────

        /// <summary>获取指定 id 的包配置</summary>
        public PackageConfigSO GetConfig(string id)
        {
            m_ConfigMap.TryGetValue(id, out var config);
            return config;
        }

        /// <summary>获取指定 id 的包配置并转型</summary>
        public T GetConfig<T>(string id) where T : PackageConfigSO
        {
            return m_ConfigMap.TryGetValue(id, out var config) && config is T typed
                ? typed
                : null;
        }

        /// <summary>尝试获取包配置</summary>
        public bool TryGetConfig(string id, out PackageConfigSO config)
        {
            return m_ConfigMap.TryGetValue(id, out config);
        }

        /// <summary>按包类型筛选所有配置</summary>
        public IEnumerable<PackageConfigSO> GetAllConfigs(PackageType type)
        {
            foreach (var kvp in m_ConfigMap)
            {
                if (kvp.Value.PackageType == type)
                    yield return kvp.Value;
            }
        }

        /// <summary>所有包配置的数量</summary>
        public int ConfigCount => m_ConfigMap.Count;

        // ── 统一加载入口 ──────────────────────────────────────

        /// <summary>通过包 id 异步加载资产</summary>
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

        /// <summary>通过 PackageConfigSO 异步加载资产</summary>
        public void LoadAssetAsync<T>(PackageConfigSO config, System.Action<T> onLoaded) where T : Object
        {
            if (config == null)
            {
                onLoaded?.Invoke(null);
                return;
            }

            // 缓存命中
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

        // ── AA 加载 ────────────────────────────────────────────

        private IEnumerator LoadFromAA<T>(string cacheKey, string address,
            System.Action<T> onLoaded) where T : Object
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(address);
            yield return handle;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations
                .AsyncOperationStatus.Succeeded)
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

        // ── AB 加载 ────────────────────────────────────────────

        private IEnumerator LoadFromAB<T>(string cacheKey, ABPackageConfigSO config,
            System.Action<T> onLoaded) where T : Object
        {
            if (string.IsNullOrEmpty(config.bundleName))
            {
                Debug.LogError($"[AddrMgr] AB 配置缺少 bundleName: {config.id}");
                onLoaded?.Invoke(null);
                yield break;
            }

#if UNITY_EDITOR
            // Editor 非运行模式：跳过打包，直接 AssetDatabase
            if (!Application.isPlaying)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(config.assetPath);
                if (asset != null)
                    m_AssetCache[cacheKey] = asset;
                onLoaded?.Invoke(asset);
                yield break;
            }
#endif

            // Runtime：从 StreamingAssets 加载 AB 包
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

            // 从 bundle 加载具体资产
            var assetReq = bundle.LoadAssetAsync<T>(config.assetPath);
            yield return assetReq;

            var result = assetReq.asset as T;
            if (result != null)
                m_AssetCache[cacheKey] = result;

            onLoaded?.Invoke(result);
        }

        // ── Default 加载 ───────────────────────────────────────

        private void LoadFromDefault<T>(string cacheKey, string loadKey,
            System.Action<T> onLoaded) where T : Object
        {
            var asset = Resources.Load<T>(loadKey);
            if (asset != null)
                m_AssetCache[cacheKey] = asset;
            onLoaded?.Invoke(asset);
        }

        // ── 路径工具 ──────────────────────────────────────────

        /// <summary>获取 AB 包在 StreamingAssets 下的完整 URL</summary>
        private static string GetBundleUrl(string bundleName)
        {
            var path = System.IO.Path.Combine(Application.streamingAssetsPath, bundleName);

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL 必须走 HTTP 协议
            return path;
#else
            // 其他平台用 file:// 协议让 UnityWebRequest 识别本地文件
            return "file://" + path;
#endif
        }

        // ── 资源释放 ──────────────────────────────────────────

        /// <summary>卸载指定 AB 包</summary>
        public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (m_BundleCache.TryGetValue(bundleName, out var bundle))
            {
                bundle.Unload(unloadAllLoadedObjects);
                m_BundleCache.Remove(bundleName);
            }
        }

        /// <summary>卸载所有 AB 包</summary>
        public void UnloadAllBundles(bool unloadAllLoadedObjects = false)
        {
            foreach (var kvp in m_BundleCache)
                kvp.Value.Unload(unloadAllLoadedObjects);
            m_BundleCache.Clear();
            m_AssetCache.Clear();
        }

        /// <summary>清除资产缓存（不影响已加载的 AB 包）</summary>
        public void ClearAssetCache()
        {
            m_AssetCache.Clear();
        }

#if UNITY_EDITOR
        /// <summary>Editor 中直接加载（不经过包管理器，用于工具脚本）</summary>
        public T LoadInEditor<T>(string assetPath) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }
#endif
    }
}
