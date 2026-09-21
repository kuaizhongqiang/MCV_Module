namespace MCV_Module.Models.Addressable
{
    /// <summary>
    /// 内容资源的类型 —— 决定运行时用哪个泛型加载。
    ///
    /// **为什么必须显式记录**：Unity 按泛型参数做类型过滤。
    /// `.png` 导入为 Sprite 时，**主资产是 Texture2D、Sprite 是子资产** ——
    /// 用 <c>LoadAssetAsync&lt;Object&gt;</c> 只会拿到 Texture2D（后续 <c>as Sprite</c> 必然为 null）；
    /// 而 `.prefab` 的主资产就是 GameObject，用 Object 取也没事。
    /// 所以图集必须记 <see cref="Sprite"/>、模型必须记 <see cref="Prefab"/>。
    ///
    /// ⚠ **只能往后追加**（值已序列化进 <c>ABPackageConfigSO.assetKind</c> 与包配置资产）。
    /// 该类由 Provider 产出、`ContentBundleTools` 写入 <c>ABPackageConfigSO.assetKind</c>，
    /// 运行时 <c>GlobalAssetsMgr</c> 按它选择泛型。
    /// </summary>
    public enum ContentAssetKind
    {
        /// <summary>贴图 —— 按 <c>Sprite</c> 泛型加载（取子资产）。</summary>
        Sprite = 0,

        /// <summary>预制体 —— 按 <c>GameObject</c> 泛型加载。</summary>
        Prefab = 1,
    }
}
