using System;
using System.Collections.Generic;
using MCV_Module.EditorTools.Common;
using MCV_Module.Interfaces;
using MCV_Module.Models.Addressable;

// WHY: CameraBg 的运行时链路固定 —— CameraBg 启动 → GlobalAssetsMgr.LoadSpritesByPackageIdsAsync → GlobalAddressableMgr 读 StreamingAssets/CameraBg/camerabg → 取 Sprite.texture → 材质 _Texture_1 / _Texture_2，故包名与两张图的顺序都不能随意改。
// WHY: 全局包之间只该差「条目从哪来 + 两条策略」；编译守卫 / 素材校验 / 写配置 / 同步清单 / 构建全部由 Assets/Editor/Common/GlobalBundleRunner 收拢，本文件不得再实现流程。
/// <summary>CameraBg 全局包 provider：固定两张背景图打成一包（bundle <c>CameraBg/camerabg</c>）。</summary>
public sealed class CameraBgGlobalProvider : IGlobalBundleProvider
{
    /// <summary>bundle 文件名段（**必须小写**：运行时会把末段强制小写，大小写不一致会在 WebGL / Linux 上 404）。</summary>
    const string BundleFileName = "camerabg";

    // WHY: 两个数组按下标一一对应，且**顺序即 CameraBg 材质的 _Texture_1 / _Texture_2**：0 = 底色，1 = 淡入切换的目标；改顺序 / 换图就改这里，改完重跑菜单即可。
    /// <summary>包配置 id ↔ 贴图工程路径（两个数组按下标一一对应）。</summary>
    static readonly string[] Ids =
    {
        "camerabg_room",
        "camerabg_contactor",
    };

    static readonly string[] AssetPaths =
    {
        "Assets/Sprites/Public/CamBg_1.jpg",
        "Assets/Sprites/Public/CamBg_2.jpg",
    };

    /// <summary>对账报告里的来源名与日志 tag（日志 tag = <c>[{Name}AB]</c>）。</summary>
    public string Name => "CameraBg";

    public string MenuPath => "MCV Build/CameraBg 背景图 AB（两张打成一包）";

    public string ConfigDir => EditorPaths.CameraBgConfigDir;

    /// <summary>bundle 名 = 相对 StreamingAssets 的路径（目录段唯一取自 <see cref="EditorPaths"/>）。</summary>
    public string BundleName => $"{EditorPaths.CameraBgBundleDirName}/{BundleFileName}";

    // WHY: 保持既有语义 —— 本包条目表固定两条，历史上从不删配置目录里的内容（对比 RoomOne / UI 自带残留清理）
    public bool CleanStaleConfigs => false;

    // WHY: 保持既有语义 —— 素材校验遇第一条问题即中止、只弹那一条（对比 RoomOne / UI 收集全部问题一次报出）
    public bool AbortOnFirstAssetProblem => true;

    /// <summary>固定两条：id 与路径按 <see cref="Ids" /> / <see cref="AssetPaths" /> 的下标对齐。</summary>
    public IEnumerable<GlobalResourceEntry> Collect()
    {
        var list = new List<GlobalResourceEntry>(Ids.Length);
        for (int i = 0; i < Ids.Length; i++)
            // WHY: assetKind 显式写 Sprite —— 不写就吃 SO 字段默认值，而该默认值与 Provider 侧默认值并不一致（P1-8）
            list.Add(new GlobalResourceEntry(Ids[i], AssetPaths[i], null, ContentAssetKind.Sprite));

        return list;
    }

    // WHY: 两张背景图只引用工程内贴图，不引用任何别的全局包 —— 分开建也不会把谁的资源复制进来，故无需同批构建
    /// <summary>无跨包依赖：本包资源不引用其它全局包的资源，可单独构建。</summary>
    public IEnumerable<IGlobalBundleProvider> BuildWith => Array.Empty<IGlobalBundleProvider>();
}
