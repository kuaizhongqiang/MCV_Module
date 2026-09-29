using System;

namespace MCV_Module.Models.Addressable
{
    // WHY: 类型即加载链路（Default→Resources.Load、AA→Addressables、AB→StreamingAssets 的 UnityWebRequest）；GlobalAddressableMgr 按它路由，改枚举值会改变资源走哪条链路。
    /// <summary>包类型枚举：标识资源的加载策略（Default / AA / AB）。</summary>
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
