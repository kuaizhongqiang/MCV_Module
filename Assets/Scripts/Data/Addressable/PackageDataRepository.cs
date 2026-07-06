using System;
using System.Collections.Generic;
using System.Linq;

namespace MCV_Module.Data.Addressable
{
    /// <summary>
    /// 包数据仓储 — 统一管理所有包类型数据的注册、查询与生命周期
    ///
    /// MCV Model 层：数据的唯一可信源（Single Source of Truth）
    /// 所有资源加载操作都应通过此仓储查询包数据和获取加载键。
    ///
    /// 初始化方式：
    ///   1. 游戏启动时加载 PackageDatabaseSO
    ///   2. 调用 InitializeFromDatabase() 将 SO 数据转换为运行时模型
    ///   3. 后续 GlobalAddressableMgr.LoadAssetAsync() 通过 id 查仓储 → 路由到对应加载器
    ///
    /// 单例访问：PackageDataRepository.Instance
    /// </summary>
    public class PackageDataRepository
    {
        // ── 单例 ──────────────────────────────────────────────
        private static readonly Lazy<PackageDataRepository> s_Instance =
            new Lazy<PackageDataRepository>(() => new PackageDataRepository());

        /// <summary>全局唯一实例（线程安全）</summary>
        public static PackageDataRepository Instance => s_Instance.Value;

        // ── 内部存储 ──────────────────────────────────────────
        /// <summary>以 id 为键的包数据字典</summary>
        private readonly Dictionary<string, PackageDataBase> m_Packages =
            new Dictionary<string, PackageDataBase>();

        // ── 事件 ──────────────────────────────────────────────

        /// <summary>包数据被注册时触发（增/改）</summary>
        public event Action<PackageDataBase> OnPackageAdded;

        /// <summary>包数据被移除时触发</summary>
        public event Action<PackageDataBase> OnPackageRemoved;

        // ── 注册 ──────────────────────────────────────────────

        /// <summary>
        /// 注册一个包数据到仓储中
        /// 如果 id 已存在则覆盖旧数据
        /// </summary>
        /// <param name="package">包数据实例</param>
        /// <exception cref="ArgumentNullException">package 为 null 时抛出</exception>
        /// <exception cref="ArgumentException">package.id 为空时抛出</exception>
        /// <returns>是否为新插入（false 表示已存在并覆盖）</returns>
        public bool Register(PackageDataBase package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));

            if (string.IsNullOrEmpty(package.id))
                throw new ArgumentException("Package must have a non-empty id", nameof(package));

            bool isNew = !m_Packages.ContainsKey(package.id);
            m_Packages[package.id] = package;
            OnPackageAdded?.Invoke(package);
            return isNew;
        }

        /// <summary>批量注册多个包数据</summary>
        public void RegisterRange(IEnumerable<PackageDataBase> packages)
        {
            foreach (var pkg in packages)
                Register(pkg);
        }

        // ── 移除 ──────────────────────────────────────────────

        /// <summary>按 id 移除包数据，触发 OnPackageRemoved 事件</summary>
        public bool Remove(string id)
        {
            if (m_Packages.TryGetValue(id, out var package))
            {
                m_Packages.Remove(id);
                OnPackageRemoved?.Invoke(package);
                return true;
            }
            return false;
        }

        /// <summary>清空所有包数据，逐个触发 OnPackageRemoved 事件</summary>
        public void Clear()
        {
            var all = m_Packages.Values.ToArray();
            m_Packages.Clear();
            foreach (var pkg in all)
                OnPackageRemoved?.Invoke(pkg);
        }

        // ── 查询 ──────────────────────────────────────────────

        /// <summary>按 id 获取包数据，未找到返回 null</summary>
        public PackageDataBase Get(string id)
        {
            m_Packages.TryGetValue(id, out var package);
            return package;
        }

        /// <summary>按 id 获取并自动转型为指定类型，未找到或类型不匹配返回 null</summary>
        public T Get<T>(string id) where T : PackageDataBase
        {
            return m_Packages.TryGetValue(id, out var package) && package is T typed
                ? typed
                : null;
        }

        /// <summary>获取所有指定类型的包数据（如 GetAll&lt;RuntimeABPackageData&gt;()）</summary>
        public IEnumerable<T> GetAll<T>() where T : PackageDataBase
        {
            return m_Packages.Values.OfType<T>();
        }

        /// <summary>按 PackageType 枚举筛选（获取所有 AA / AB / Default 包）</summary>
        public IEnumerable<PackageDataBase> GetAll(PackageType type)
        {
            return m_Packages.Values.Where(p => p.PackageType == type);
        }

        /// <summary>获取所有包数据的只读视图</summary>
        public IEnumerable<PackageDataBase> All => m_Packages.Values;

        /// <summary>当前仓储中包的总数</summary>
        public int Count => m_Packages.Count;

        /// <summary>判断指定 id 是否已注册</summary>
        public bool Contains(string id) => m_Packages.ContainsKey(id);

        // ── 批量查询 ──────────────────────────────────────────

        /// <summary>通过多个 id 批量获取，自动跳过未找到的条目</summary>
        public IReadOnlyList<PackageDataBase> GetMultiple(IEnumerable<string> ids)
        {
            return ids.Select(Get).Where(p => p != null).ToList();
        }

        /// <summary>通过加载键反向查找包数据（通常用于调试）</summary>
        public PackageDataBase FindByLoadKey(string loadKey)
        {
            return m_Packages.Values.FirstOrDefault(p => p.GetLoadKey() == loadKey);
        }

        // ── 初始化辅助 ─────────────────────────────────────────

        /// <summary>
        /// 从枚举数据批量初始化（常用于单元测试或手动构建数据时）
        /// 会先 Clear() 再注册
        /// </summary>
        public void Initialize(IEnumerable<PackageDataBase> packages)
        {
            Clear();
            RegisterRange(packages);
        }

        /// <summary>
        /// 从 PackageDatabaseSO 初始化运行时注册表（推荐方式）
        ///
        /// 遍历 SO 列表中的每个 PackageConfigSO，根据其 PackageType 创建对应的
        /// 轻量运行时数据类（RuntimeAAPackageData / RuntimeABPackageData / RuntimeDefaultPackageData），
        /// 提取关键字段后注册到仓储中。
        ///
        /// 此方法应在游戏启动时调用一次。
        /// </summary>
        /// <param name="database">包含所有包配置的主清单 SO</param>
        /// <exception cref="ArgumentNullException">database 为 null 时抛出</exception>
        public void InitializeFromDatabase(PackageDatabaseSO database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            Clear();

            foreach (var config in database.packages)
            {
                if (config == null || string.IsNullOrEmpty(config.id))
                    continue;

                PackageDataBase entry = config.PackageType switch
                {
                    PackageType.AA => new RuntimeAAPackageData
                    {
                        id = config.id,
                        displayName = config.displayName,
                        address = ((AAPackageConfigSO)config).address,
                    },
                    PackageType.AB => new RuntimeABPackageData
                    {
                        id = config.id,
                        displayName = config.displayName,
                        bundleName = ((ABPackageConfigSO)config).bundleName,
                        assetPath = ((ABPackageConfigSO)config).assetPath,
                    },
                    PackageType.Default => new RuntimeDefaultPackageData
                    {
                        id = config.id,
                        displayName = config.displayName,
                    },
                    _ => null,
                };

                if (entry != null)
                    Register(entry);
            }
        }

        // ── 运行时轻量数据类 ──────────────────────────────────

        /// <summary>
        /// AA 包运行时数据 — 存储 address 作为加载键
        /// 由 InitializeFromDatabase 自动创建
        /// </summary>
        internal class RuntimeAAPackageData : PackageDataBase
        {
            /// <summary>Addressables 系统中的资源地址（PackagesConfigSO.address）</summary>
            public string address;
            public override string GetLoadKey() => address;
        }

        /// <summary>
        /// AB 包运行时数据 — 存储 bundleName 和 assetPath
        /// 由 InitializeFromDatabase 自动创建
        /// GlobalAddressableMgr 会用 assetPath 在 Editor 下走 AssetDatabase 加载
        /// </summary>
        internal class RuntimeABPackageData : PackageDataBase
        {
            /// <summary>AB 包文件名（不含扩展名）</summary>
            public string bundleName;
            /// <summary>资产在项目中的完整路径（如 Assets/Art/BG/main_menu_bg.jpg）</summary>
            public string assetPath;
            /// <summary>加载键格式：bundleName:assetPath</summary>
            public override string GetLoadKey() => $"{bundleName}:{assetPath}";
        }

        /// <summary>
        /// Default 包运行时数据 — 直接用 id 作为加载键
        /// 对应 Resources.Load(id) 的路径
        /// </summary>
        internal class RuntimeDefaultPackageData : PackageDataBase
        {
            public override string GetLoadKey() => id;
        }
    }
}
