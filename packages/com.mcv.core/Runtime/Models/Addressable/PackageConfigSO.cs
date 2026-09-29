using UnityEngine;

namespace MCV_Module.Models.Addressable
{
    // WHY: 基类与 3 个具体子类必须拆成 4 个同名文件 —— 子类与基类同文件时 CreateInstance + CreateAsset 出来的资产 m_Script 为 0（脚本丢失、加载为 null，表现为「配置生成了但数据库收不到」）。
    /// <summary>包配置基类：一个 .asset 描述一个可打包资源，Editor 打包工具与运行时加载共用。</summary>
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
                 "Default 模式：此引用需放在 Resources 目录下\n\n" +
                 "⚠ 本字段是**直接引用**：配置资产位于 Resources/ 下时，被它引用的资源会被一并打进 resources.assets，\n" +
                 "   资源变两份、AB 打包失去意义。因此 AB 流水线只写 assetPath / bundleName 两个字符串，本字段留空")]
        public Object sourceAsset;

        /// <summary>包类型（AA / AB / Default），由子类固定返回</summary>
        public abstract PackageType PackageType { get; }

        /// <summary>运行时加载键：AA → address，AB → bundleName:assetPath，Default → id。</summary>
        public abstract string GetLoadKey();

#if UNITY_EDITOR
        /// <summary>目标资源在项目中的相对路径（只读），由 sourceAsset 推导，供打包工具使用。</summary>
        public string SourceAssetPath =>
            UnityEditor.AssetDatabase.GetAssetPath(sourceAsset);
#endif
    }
}
