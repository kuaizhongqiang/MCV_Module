using UnityEngine;

namespace MCV_Module.Models.Addressable
{
    // ─────────────────────────────────────────────────────────────
    //  AB（AssetBundle）配置
    //  ⚠ 独立文件：Unity 只给「文件名 = 类名」的类生成 MonoScript，派生 SO 不能和基类写在同一个文件里
    // ─────────────────────────────────────────────────────────────

    [CreateAssetMenu(
        fileName = "AB_",
        menuName = "MCV/Package/AssetBundle (AB)",
        order = 20)]
    public class ABPackageConfigSO : PackageConfigSO
    {
        [Header("AssetBundle 设置")]
        [Tooltip("归属的 ProjectClip.id（如 clip_contactor）\n\n" +
                 "内容 AB 流水线按它聚合：一个 clip 一个包，加载与卸载都以 clip 为单位\n" +
                 "由 Assets/Editor/ContentBundleTools 自动填；非内容资源（如相机背景图）留空")]
        public string clipId;

        [Tooltip("资源类型 —— 决定运行时用哪个泛型加载\n\n" +
                 "必须准确：.png 导入为 Sprite 时主资产是 Texture2D、Sprite 是子资产，\n" +
                 "用 Object 泛型只会拿到 Texture2D（as Sprite 永远为 null）\n" +
                 "由 Assets/Editor/ContentBundleTools 按 Provider 写入；非内容资源忽略")]
        public ContentAssetKind assetKind = ContentAssetKind.Sprite;

        [Tooltip("AssetBundle 名 = 相对 StreamingAssets 的路径（不含扩展名）\n\n" +
                 "打包时资源被分配到此 Bundle 中\n" +
                 "运行时从 StreamingAssets/{bundleName} 加载该 Bundle\n" +
                 "注意文件名段用**小写**：GlobalAddressableMgr 会把末段强制小写，工具写盘却按原样保留，\n" +
                 "两者不一致时 Windows 看不出问题，但 WebGL / Linux 上必然 404")]
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
}
