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
                m_SceneHandles[sceneName] = handle; // 保留 handle，便于卸载时释放包
                Log.Verbose($"[AA] AA 场景加载成功: {sceneName}, address={address}");
            }
            else
            {
                Log.Error($"[AA] AA 场景加载失败: {sceneName}, address={address}, error={handle.OperationException}");
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

        // ── 内容包（一个 ProjectClip 一个 bundle，按 clipId 聚合） ──

        /// <summary>
        /// 按归属聚合配置：返回 <see cref="ABPackageConfigSO.clipId"/> == clipId 的全部配置。
        /// 内容包的 loader（<c>GlobalAssetsMgr</c>）靠它实现「一个 clip 一次加载 / 一次卸载」。
        /// </summary>
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

        /// <summary>
        /// 卸载一个 clip：**先失效资源缓存，再卸 bundle**（<c>UnloadBundle(..., true)</c> 会连带销毁属于该包的存活实例）。
        ///
        /// 缓存必须先清：<see cref="LoadAssetRoutine{T}"/> 命中缓存时不判「是否已销毁」，
        /// 残留条目会让下一次取用拿到 destroyed 对象（MissingReferenceException / 图片空白）。
        /// 返回失效的缓存条数。
        /// </summary>
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

        /// <summary>
        /// 同步读取已缓存资源（**不触发加载**）。未加载、类型不符、或对象已被销毁都返回 false
        /// —— 已销毁的 Unity 对象在 C# 里是非 null 的包装壳，必须靠 Unity 的 <c>== null</c> 判活。
        /// </summary>
        public bool TryGetCached<T>(string packageId, out T asset) where T : Object
        {
            asset = null;
            if (string.IsNullOrEmpty(packageId)) return false;
            if (!m_AssetCache.TryGetValue(packageId, out var cached)) return false;

            asset = cached as T;
            return asset != null;
        }

        /// <summary>
        /// 按 **bundle 名** 卸载：先失效该 bundle 下所有包配置的资源缓存，再卸 bundle。
        ///
        /// <para>给**全局包**用（没有 clipId、走不了按 clip 卸载的那类包）。
        /// 缓存必须先清的理由：<see cref="LoadAssetRoutine{T}"/> 命中缓存时不判「是否已销毁」，
        /// 残留条目会让下一次取用拿到 destroyed 对象。</para>
        ///
        /// <para>bundle 名为空时返回 0 且**不卸任何东西**（防止空串误命中）。返回失效的缓存条数。</para>
        /// </summary>
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

        /// <summary>
        /// 按「包配置 id」批量加载（**保序**）：逐个加载，单个失败只记日志并跳过，不中断其余项。
        /// </summary>
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
                    Log.Error($"[AddrMgr] 未找到包配置: {id}（检查包清单是否收录该配置）");
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
        //
        // 设计说明：实例化统一用 Object.Instantiate，而不是 Addressables.InstantiateAsync ——
        //   ① 本管线把资源生命周期归「包」管（缓存/卸载都按包整体进行），不需要 AA 的逐实例引用计数；
        //   ② 对象池要求实例可自由 Destroy 后复用，AA 的 ref-count 实例化会与之冲突；
        //   ③ 三种 PackageType（AA/AB/Default）走同一套实例化口径，行为一致、易排查。

        /// <summary>已登记的存活实例数（顺带清理被外部销毁的空槽）。</summary>
        public int InstanceCount
        {
            get { PruneInstances(); return m_InstanceMap.Count; }
        }

        /// <summary>
        /// 由「已加载的预制体」实例化一个对象并登记包归属。
        /// 供对象池的实例工厂回调使用（预制体已由 LoadAssetAsync 缓存，无需再走异步）。
        /// </summary>
        /// <param name="packageId">包配置 id（登记用，卸载该包时会一并销毁这些实例）</param>
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

        /// <summary>
        /// 销毁经本管理器实例化的对象并注销登记。
        /// 注意：池化实例请走池的归还接口（归还 ≠ 销毁），只在真正不需要时才调这里。
        /// </summary>
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

        /// <summary>
        /// 卸载单个 AssetBundle。
        /// <paramref name="unloadAllLoadedObjects"/> = true 时先销毁依赖该包的存活实例，
        /// 否则包卸载后实例的网格/材质会变成 missing（粉色）。
        /// </summary>
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

            // Inspector 未挂清单时走 Resources 兜底（与 BuildSceneMap 的 SceneAAConfig 同一口径）：
            // 新增包配置只要保证 Resources/Config/PackageDB_Master.asset 在位即可，不必改 Manager.prefab
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
            // Unity 构建时会把 bundle 的**文件名**转小写（目录保持原样，见构建产物里的 AssetBundleManifest），
            // 这里同样只小写最后一段，避免大小写敏感的平台（Linux / Android / WebGL 静态服务器）上找不到文件。
            string normalized = bundleName;
            int lastSlash = normalized.LastIndexOf('/');
            normalized = lastSlash >= 0
                ? normalized.Substring(0, lastSlash + 1) + normalized.Substring(lastSlash + 1).ToLowerInvariant()
                : normalized.ToLowerInvariant();

            var path = System.IO.Path.Combine(Application.streamingAssetsPath, normalized);
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL 下 StreamingAssets 本身就是 HTTP 路径，直接拼 URL（不能加 file://）
            return path.Replace("\\", "/");
#else
            // 本地走 file:// URL：用 Uri 做转义，避免工程路径含空格/中文时加载失败
            return new System.Uri(path).AbsoluteUri;
#endif
        }
        #endregion
    }
}
