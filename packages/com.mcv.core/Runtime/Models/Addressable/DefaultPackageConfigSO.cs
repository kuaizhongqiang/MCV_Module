using UnityEngine;

namespace MCV_Module.Models.Addressable
{
    // WHY: Unity 只给「文件名 = 类名」的类生成 MonoScript，派生 SO 必须独占一个与类同名的文件，否则 CreateAsset 出来的资产 m_Script 为 0（脚本丢失）。
    /// <summary>Default（本地直引用）配置：资源走 Resources / 序列化引用，加载键即 id。</summary>
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
