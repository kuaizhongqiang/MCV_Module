using UnityEngine;

namespace MCV_Module.Models.Addressable
{
    // ─────────────────────────────────────────────────────────────
    //  AA（Addressable Assets）配置
    //  ⚠ 独立文件：Unity 只给「文件名 = 类名」的类生成 MonoScript，派生 SO 不能和基类写在同一个文件里
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

        [Tooltip("Addressables 组名（Editor 用，运行时不需要）\n\n" +
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
}
