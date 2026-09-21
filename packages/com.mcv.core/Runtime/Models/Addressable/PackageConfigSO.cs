using UnityEngine;

namespace MCV_Module.Models.Addressable
{
    /// <summary>
    /// 包配置基类 — 每种可打包资源对应一个 .asset 文件
    ///
    /// 双重用途：
    ///   1. Editor 打包工具遍历此数据，构建 AA Group / AB Bundle
    ///   2. 运行时读取此数据，驱动资源加载
    ///
    /// 使用方式：在 Assets 右键 → Create → MCV → Package → 选择合适的类型
    ///
    /// ⚠ 拆文件说明（2026-09-21）：Unity 只给「文件名 = 类名」的类生成 MonoScript。
    /// 原实现把基类与 3 个具体子类都写在 `PackageConfigSO.cs` 里，导致
    /// `CreateInstance&lt;ABPackageConfigSO&gt;() + CreateAsset` 出来的资产 `m_Script: {fileID: 0}`（脚本丢失、加载为 null，
    /// 表现为「配置生成了但数据库收不到」）。因此拆成 4 个同名文件，每个文件只放一个类：
    /// 本文件（PackageConfigSO）+ AAPackageConfigSO.cs + ABPackageConfigSO.cs + DefaultPackageConfigSO.cs。
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
}
