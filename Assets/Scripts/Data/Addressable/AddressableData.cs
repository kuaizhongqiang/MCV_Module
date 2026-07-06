using System;

namespace MCV_Module.Data.Addressable
{
    /// <summary>
    /// 包类型枚举 — 标识资源的加载策略
    ///
    /// 每种类型对应一套完整的加载链路：
    ///   Default → Resources.Load （本地直引用，随场景加载）
    ///   AA      → Addressables.LoadAssetAsync （Addressable Assets 系统，支持热更和依赖管理）
    ///   AB      → UnityWebRequest → AssetBundle （传统 AssetBundle，从 StreamingAssets 加载）
    /// </summary>
    [Serializable]
    public enum PackageType
    {
        /// <summary>本地直引用（Resources / Serialized Reference），不经过包管理系统</summary>
        Default,
        /// <summary>Addressable Assets 系统，支持远程热更、依赖管理、引用计数</summary>
        AA,
        /// <summary>传统 AssetBundle，从 StreamingAssets 加载，手动管理生命周期</summary>
        AB,
    }

    /// <summary>
    /// 包数据运行时基类
    ///
    /// 这是 Loader 层（GlobalAddressableMgr）获取加载信息的统一接口。
    /// 派生类（RuntimeAAPackageData / RuntimeABPackageData / RuntimeDefaultPackageData）
    /// 由 PackageDataRepository.InitializeFromDatabase() 根据 PackageConfigSO 自动创建。
    ///
    /// 外部代码通常不需要直接操作此类，通过 GlobalAddressableMgr.LoadAssetAsync() 即可。
    /// </summary>
    public abstract class PackageDataBase : DataBase
    {
        /// <summary>包类型（AA / AB / Default），决定资源的加载方式</summary>
        public PackageType PackageType { get; protected set; }

        /// <summary>
        /// 获取运行时加载键
        ///
        /// 返回值格式因包类型而异：
        ///   AA → address 字符串（Addressables 地址）
        ///   AB → bundleName:assetPath（冒号分隔）
        ///   Default → id 或 Resources 路径
        ///
        /// 此值由 GlobalAddressableMgr 内部解析和使用。
        /// </summary>
        public abstract string GetLoadKey();
    }
}
