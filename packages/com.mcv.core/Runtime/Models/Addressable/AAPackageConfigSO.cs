using UnityEngine;

namespace MCV_Module.Models.Addressable
{
    // WHY: Unity 只给「文件名 = 类名」的类生成 MonoScript，派生 SO 必须独占一个与类同名的文件，否则 CreateAsset 出来的资产 m_Script 为 0（脚本丢失）。
    /// <summary>Addressable Assets（AA）配置：声明资源地址、标签与 AA Group 名，运行时按 address 加载。</summary>
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

        [Tooltip("资源的标签，用于批量加载或分组筛选\n\n" +
                 "例如：bg、main_menu、character 等\n" +
                 "运行时可以通过标签一次性加载一组资源")]
        public string[] labels;

        [Tooltip("Addressables 组名（Editor 用，运行时不需要）\n\n" +
                 "构建 AA Group 时，资源会被分配到同名的 Group 中")]
        public string groupName;

        public override PackageType PackageType => PackageType.AA;
        public override string GetLoadKey() => address;

        // WHY: 只在 address 为空时填，避免覆盖手工填写的地址。
        /// <summary>Editor 工具：address 为空时用 sourceAsset 的文件名（不含扩展名）填充。</summary>
        public void AutoAssignAddress()
        {
            if (string.IsNullOrEmpty(address) && sourceAsset != null)
                address = sourceAsset.name;
        }
    }
}
