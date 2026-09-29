using System;
using System.Collections.Generic;
using MCV_Module.EditorTools.Common;
using MCV_Module.Interfaces;
using MCV_Module.Models.Addressable;

// WHY: 用全局包而非内容包 —— 8 张图同进同走，故 ABPackageConfigSO.clipId 一律留空（Runner 统一写 null），与 CameraBg/camerabg 同形态（见 Docs/design_ai/BundlePipeline.md §8）；GlobalAddressableMgr.GetConfigsByClip 对空 clipId 直接返回空表，故不会被「进内容页」链路顺带加载/卸载。
// WHY: 包配置 id = roomone_{器件}（器件 = clip.id 去 clip_ 前缀），由 ContentNaming.RoomIconId 推导，运行时同一方法即可按 clip 找到图标，无需第二张映射表。
// WHY: 输出目录独立 StreamingAssets/RoomOne/ —— 不能挂进 Content/，否则内容流水线构建时 sweepStale 会清掉「不属于本次产出」的产物。
/// <summary>RoomOne 全局包 provider：漫游房间 8 块项目 HUD 的图标打成一包（bundle <c>RoomOne/roomone</c>）。</summary>
public sealed class RoomOneGlobalProvider : IGlobalBundleProvider
{
    /// <summary>图标素材目录。</summary>
    const string IconRoot = "Assets/Sprites/Origin/Menu";

    // WHY: 这句话必须留在缺素材的问题里 —— 表里写的是工程实际文件名，大小写不统一，逐字比对是这套固定表最容易踩的坑；Runner 会把它作为括号备注附在问题文案后，故文案与重构前逐字一致。
    /// <summary>素材缺失时的括号备注。</summary>
    const string FileNameHint = "表中文件名与工程实际文件名（含大小写）必须逐字一致";

    // WHY: 表里写的是工程里的实际文件名（注意大小写不统一），换图/改归属就改这张表后重跑本菜单；裁掉一条后重跑会自动删掉它遗留的包配置（残留清理由 Runner 按 CleanStaleConfigs 执行）。
    /// <summary>一条房间图标：器件段 + 素材文件名（取自需求「漫游页 · Hud 投影」表：Pic-jcq01 接触器、Pic-rjdq02 热继电器、Pic-sjjdq03 时间继电器、Pic-zldq04 主令电器、Pic-rdq02 熔断器、Pic-dlq03 断路器、Pic-sdjdq05 速度继电器、Pic-zhkg03 组合开关）。</summary>
    sealed class IconEntry
    {
        public readonly string Device;      // WHY: contactor（= clip.id 去 clip_ 前缀，小驼峰）
        public readonly string FileName;    // pic-jcq01.png

        public IconEntry(string device, string fileName)
        {
            Device = device;
            FileName = fileName;
        }

        /// <summary>包配置 id：roomone_contactor。</summary>
        public string Id { get { return ContentNaming.RoomIconIdOfDevice(Device); } }

        /// <summary>工程路径：Assets/Sprites/Origin/Menu/pic-jcq01.png。</summary>
        public string AssetPath { get { return IconRoot + "/" + FileName; } }
    }

    // WHY: 房间 1 的 HUD 不含小测验（已定），Pic-xcy.png 因此没有消费方，不进包 —— 留在包里只会白占体积、还会留下一个没人用的包配置。
    /// <summary>房间图标条目，只有 8 条。</summary>
    static readonly IconEntry[] Icons =
    {
        new IconEntry("contactor",         "pic-jcq01.png"),    // 接触器
        new IconEntry("thermalRelay",      "Pic-rjdq02.png"),   // 热继电器
        new IconEntry("timeRelay",         "Pic-sjjdq03.png"),  // 时间继电器
        new IconEntry("masterAppliance",   "Pic-zldq04.png"),   // 主令电器
        new IconEntry("fuse",              "Pic-rdq02.png"),    // 熔断器
        new IconEntry("breaker",           "Pic-dlq03.png"),    // 断路器
        new IconEntry("speedRelay",        "Pic-sdjdq05.png"),  // 速度继电器
        new IconEntry("combinationSwitch", "Pic-zhkg03.png"),   // 组合开关
    };

    /// <summary>对账报告里的来源名与日志 tag（日志 tag = <c>[{Name}AB]</c>）。</summary>
    public string Name => "RoomOne";

    public string MenuPath => "MCV Build/RoomOne 房间图标 AB（八张打成一包）";

    public string ConfigDir => EditorPaths.RoomOneConfigDir;

    /// <summary>bundle 名 = RoomOne/roomone（目录段取自 <see cref="ContentNaming.RoomOneBundleDirName"/>，文件名段已小写）。</summary>
    public string BundleName { get { return ContentNaming.RoomOneBundleName; } }

    // WHY: 自带残留清理 —— 裁掉一条图标后它遗留的 AB_xx.asset 若不删就成了「配置在、包里没有」的可解析 id（运行期取到 null），且会一直被 AutoCollect 收回主清单
    public bool CleanStaleConfigs => true;

    // WHY: 固定表一次能报出全部问题（缺素材 / 导入类型不对 / id 重复），逐条修比重跑 8 次菜单快，故不用「遇第一条即中止」
    public bool AbortOnFirstAssetProblem => false;

    /// <summary>按 <see cref="Icons" /> 表产出条目（顺序即表内顺序）。</summary>
    public IEnumerable<GlobalResourceEntry> Collect()
    {
        var list = new List<GlobalResourceEntry>(Icons.Length);
        foreach (IconEntry icon in Icons)
            // WHY: assetKind 必须显式写 Sprite —— 不写就吃 SO 字段默认值，而两边默认值并不一致
            list.Add(new GlobalResourceEntry(icon.Id, icon.AssetPath, FileNameHint, ContentAssetKind.Sprite));

        return list;
    }

    // WHY: 8 张房间图标只引用工程内贴图，不引用任何别的全局包 —— 分开建也不会把谁的资源复制进来，故无需同批构建
    /// <summary>无跨包依赖：本包资源不引用其它全局包的资源，可单独构建。</summary>
    public IEnumerable<IGlobalBundleProvider> BuildWith => Array.Empty<IGlobalBundleProvider>();
}
