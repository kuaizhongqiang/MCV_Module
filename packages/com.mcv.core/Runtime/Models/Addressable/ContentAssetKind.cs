namespace MCV_Module.Models.Addressable
{
    // WHY: .png 导入为 Sprite 时主资产是 Texture2D、Sprite 是子资产，用 Object 泛型只会拿到 Texture2D（as Sprite 为 null）；图集必须记 Sprite、模型必须记 Prefab，否则图集加载为空。
    // WHY: 纠正旧说法 —— assetKind 绝不是「对本包无意义的假值」：Editor 流水线的素材校验（GlobalBundleRunner.Validate 按 kind 分支）与全局包预加载（GlobalAssetsMgr.PreloadGlobalBundleRoutine 按 assetKind 分流泛型）都按它分流，写错就是加载拿到 null。
    /// <summary>内容资源类型：决定运行时用哪个泛型加载（Sprite / Prefab / Font / TmpFont）。</summary>
    public enum ContentAssetKind
    {
        /// <summary>贴图 —— 按 <c>Sprite</c> 泛型加载（取子资产）。</summary>
        Sprite = 0,

        /// <summary>预制体 —— 按 <c>GameObject</c> 泛型加载。</summary>
        Prefab = 1,

        // WHY: 下面两项**只服务「字体全局包」**（B3）；内容包（按 clip）的 provider 永远不会产出它们，故内容装卸链路（LoadClipRoutine 的「Sprite / 其余 GameObject」二分支）行为完全不变。
        // 枚举值只能往后追加、不能改现有值：assetKind 以整数序列化进既有 ABPackageConfigSO 资产，改值会让老配置的含义整体错位。
        /// <summary>Legacy 字体 —— 按 <c>UnityEngine.Font</c> 泛型加载（仅字体全局包产出）。</summary>
        Font = 2,

        /// <summary>TMP 字体资产 —— 按 <c>TMPro.TMP_FontAsset</c> 泛型加载（仅字体全局包产出）。</summary>
        TmpFont = 3,
    }
}
