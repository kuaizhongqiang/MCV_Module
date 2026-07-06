using UnityEngine;

namespace MCV_Module.Data.Addressable
{
    /// <summary>
    /// 包配置基类 — 每种可打包资源对应一个 .asset 文件
    ///
    /// 双重用途：
    ///   1. Editor 打包工具遍历此数据，构建 AA Group / AB Bundle
    ///   2. 运行时读取此数据，驱动资源加载
    ///
    /// 使用方式：在 Assets 右键 → Create → MCV → Package → 选择合适的类型
    /// </summary>
    public abstract class PackageConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("资产的唯一标识符，运行时通过此 id 查找和加载该资源，请确保不与其他包重复")]
        public string id;

        [Tooltip("资产的显示名称，用于 Editor 识别和日志输出，不影响运行时加载")]
        public string displayName;

        [Header("目标资源")]
        [Tooltip("要打包的实际资产（贴图、预制体、音频等），Editor 中直接拖拽引用即可\n\n" +
                 "AA 模式：此引用用于打包时构建 Addressables Group\n" +
                 "AB 模式：此引用用于打包时分配到 AssetBundle\n" +
                 "Default 模式：此引用需放在 Resources 目录下")]
        public Object sourceAsset;

        /// <summary>包类型（AA / AB / Default），由子类固定返回</summary>
        public abstract PackageType PackageType { get; }

        /// <summary>
        /// 运行时加载键
        /// AA → address（Addressables 地址）
        /// AB → bundleName:assetPath（包名:资源路径）
        /// Default → id（Resources 路径）
        /// </summary>
        public abstract string GetLoadKey();

#if UNITY_EDITOR
        /// <summary>
        /// 目标资源在项目中的相对路径（只读）
        /// 例如：Assets/Art/BG/main_menu_bg.jpg
        /// 此路径由 sourceAsset 自动推导，供打包工具使用
        /// </summary>
        public string SourceAssetPath =>
            UnityEditor.AssetDatabase.GetAssetPath(sourceAsset);
#endif
    }

    // ─────────────────────────────────────────────────────────────
    //  AA（Addressable Assets）配置
    // ─────────────────────────────────────────────────────────────

    [CreateAssetMenu(
        fileName = "AA_",
        menuName = "MCV/Package/Addressable (AA)",
        order = 10)]
    public class AAPackageConfigSO : PackageConfigSO
    {
        [Header("Addressable 设置")]
        [Tooltip("Addressables 系统中的资源地址（运行时加载的唯一标识）\n\n" +
                 "例如：bg/main_menu_bg\n" +
                 "建议按类别分层命名，方便分组和管理")]
        public string address;

        [Tooltip("资源的标签，用于批量加载或分组筛选\n" +
                 "例如：bg、main_menu、character 等\n" +
                 "运行时可以通过标签一次性加载一组资源")]
        public string[] labels;

        [Tooltip("Addressables 组名（Editor 用，运行时不需要）\n" +
                 "构建 AA Group 时，资源会被分配到同名的 Group 中")]
        public string groupName;

        public override PackageType PackageType => PackageType.AA;
        public override string GetLoadKey() => address;

        /// <summary>
        /// Editor 工具方法：自动将 address 填充为目标资源的文件名（不含扩展名）
        /// 例如 sourceAsset 为 main_menu_bg.jpg → address = "main_menu_bg"
        /// </summary>
        public void AutoAssignAddress()
        {
            if (string.IsNullOrEmpty(address) && sourceAsset != null)
                address = sourceAsset.name;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  AB（AssetBundle）配置
    // ─────────────────────────────────────────────────────────────

    [CreateAssetMenu(
        fileName = "AB_",
        menuName = "MCV/Package/AssetBundle (AB)",
        order = 20)]
    public class ABPackageConfigSO : PackageConfigSO
    {
        [Header("AssetBundle 设置")]
        [Tooltip("AssetBundle 文件名（不含扩展名）\n\n" +
                 "打包时资源被分配到此 Bundle 中\n" +
                 "运行时从 StreamingAssets/{bundleName} 加载该 Bundle")]
        public string bundleName;

        [Tooltip("AB 包变体标识（可选）\n" +
                 "例如：hd / sd，用于区分同一资源的不同精度版本")]
        public string variant;

        [Tooltip("资产的完整项目路径（如 Assets/Art/BG/main_menu_bg.jpg）\n\n" +
                 "Editor 下可通过 AutoAssignPath() 自动填充\n" +
                 "运行时由此路径在 Bundle 内查找资源")]
        public string assetPath;

        public override PackageType PackageType => PackageType.AB;

        /// <summary>
        /// 加载键格式：bundleName:assetPath
        /// 运行时 GlobalAddressableMgr 按此格式解析：
        ///   冒号前 → AB 包名（定位 StreamingAssets 中的文件）
        ///   冒号后 → 资源在包内的项目路径（加载具体资源）
        /// </summary>
        public override string GetLoadKey() => $"{bundleName}:{assetPath}";

#if UNITY_EDITOR
        /// <summary>
        /// Editor 工具方法：自动将 assetPath 填充为 sourceAsset 的项目相对路径
        /// </summary>
        public void AutoAssignPath()
        {
            if (string.IsNullOrEmpty(assetPath) && sourceAsset != null)
                assetPath = UnityEditor.AssetDatabase.GetAssetPath(sourceAsset);
        }
#endif
    }

    // ─────────────────────────────────────────────────────────────
    //  Default（本地直引用）配置
    // ─────────────────────────────────────────────────────────────

    [CreateAssetMenu(
        fileName = "Default_",
        menuName = "MCV/Package/Default (Local)",
        order = 30)]
    public class DefaultPackageConfigSO : PackageConfigSO
    {
        public override PackageType PackageType => PackageType.Default;
        public override string GetLoadKey() => id;
    }
}
