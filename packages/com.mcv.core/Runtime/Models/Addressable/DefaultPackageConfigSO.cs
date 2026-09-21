using UnityEngine;

namespace MCV_Module.Models.Addressable
{
    // ─────────────────────────────────────────────────────────────
    //  Default（本地直引用）配置
    //  ⚠ 独立文件：Unity 只给「文件名 = 类名」的类生成 MonoScript，派生 SO 不能和基类写在同一个文件里
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
