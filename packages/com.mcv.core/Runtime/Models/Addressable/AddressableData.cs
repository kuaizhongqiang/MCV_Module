using System;

namespace MCV_Module.Models.Addressable
{
    /// <summary>
    /// 包类型枚举 — 标识资源的加载策略
    ///
    /// 每种类型对应一套完整的加载链路：
    ///   Default → Resources.Load （本地直引用，随场景加载）
    ///   AA      → Addressables.LoadAssetAsync （Addressable Assets 系统，支持热更和依赖管理）
    ///   AB      → UnityWebRequest → AssetBundle （传统 AssetBundle，从 StreamingAssets 加载）
    ///
    /// 运行时加载由 GlobalAddressableMgr 按该类型路由（AA / AB / Default）。
    /// 注：原 PackageDataRepository / PackageDataBase（运行时注册表）无任何调用方，已移除；
    /// 配置字典（id → PackageConfigSO）由 GlobalAddressableMgr.BuildConfigMap 自建。
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
}
