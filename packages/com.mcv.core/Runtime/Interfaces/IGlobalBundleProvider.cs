using System.Collections.Generic;
using MCV_Module.Models.Addressable;

namespace MCV_Module.Interfaces
{
    // WHY: 全局包没有 clip，id 由 provider 自己拼（如 ui_TipsPanel）；kind 必须准——运行时按它选泛型加载，图集是 Sprite 子资产。
    /// <summary>待打包的一条全局资源：全局包流水线的中间产物（与 ProjectClip 无关）。</summary>
    public class GlobalResourceEntry
    {
        /// <summary>包配置 id（= 运行时取件键）。</summary>
        public string id;

        /// <summary>资源工程路径（<c>Assets/...</c>）。为空表示该条目指向的素材缺失。</summary>
        public string assetPath;

        // WHY: 资源类型必须准——运行时按它选泛型加载，图集是 Sprite 子资产，用 Object 取只会拿到 Texture2D。
        /// <summary>资源类型：运行时按它选泛型加载。</summary>
        public ContentAssetKind kind = ContentAssetKind.Prefab;

        /// <summary>对账报告里的备注（来源说明 / 缺失原因等）。</summary>
        public string note;

        public GlobalResourceEntry() { }

        public GlobalResourceEntry(string id, string assetPath, string note = null,
                                   ContentAssetKind kind = ContentAssetKind.Prefab)
        {
            this.id = id;
            this.assetPath = assetPath;
            this.note = note;
            this.kind = kind;
        }
    }

    // WHY: 在第三个全局包出现之前，CameraBg / RoomOne 各是一个专用工具（校验→写配置→同步清单→构建→残留清理这 8 段几乎逐行重复）。本抽象只留「条目从哪来 + 叫什么 + 三条策略」加一条跨包依赖，其余全部收进 Editor/Common/GlobalBundleRunner。
    // WHY: clipId 一律留空 —— GlobalAddressableMgr.GetConfigsByClip 对空 clipId 返回空表，故全局包天然常驻、不会被按 clip 的装卸链路带走（UnloadClip 按 clipId 过滤）。
    /// <summary>全局包提供者（Editor-only 契约）：一个全局 bundle 一组资源，不按 ProjectClip 收集。</summary>
    public interface IGlobalBundleProvider
    {
        /// <summary>对账报告里的来源名与日志 tag（如 <c>UI</c>）。</summary>
        string Name { get; }

        /// <summary>工具菜单项路径（如 <c>MCV Build/UI prefab AB</c>）。</summary>
        string MenuPath { get; }

        /// <summary>包配置目录（如 <c>Assets/Resources/Config/UIPackages</c>）。</summary>
        string ConfigDir { get; }

        // WHY: 末段必须全小写 —— GlobalAddressableMgr.GetBundleUrl 会把文件名段强制小写，大小写不一致会在 WebGL / Linux 上 404。
        /// <summary>bundle 名（如 <c>UI/ui</c>）；输出目录 = <c>Assets/StreamingAssets/{首段}</c>，必须是独立段。</summary>
        string BundleName { get; }

        /// <summary>收集本包的全部资源条目。</summary>
        IEnumerable<GlobalResourceEntry> Collect();

        // WHY: 实测（TipsPanel.prefab 嵌套基座 prefab，基座直引 SIMHEI.TTF）：
        //   ① 只声明面板包（本次 BuildAssetBundles 的分配表里没有字体包）→ 面板包 6187 KB（字体被**隐式复制**进来）；
        //   ② 同一次 BuildAssetBundles 里同时声明「面板包 + 字体包」→ 面板 152 KB，字体包 6033 KB（面板只记**依赖**）。
        // 结论：Unity 的「同一资产被显式分配后不再复制」**只在同一次构建调用内成立**。GlobalBundleRunner 是一个 provider 建一个包，
        // 若字体包先建、UI 包后建（两次 BuildAssetBundles），字体仍会被复制进 UI 包（两份、体积翻倍），
        // 「两套字体在字体包里各存一份」不可能成立。故这里显式声明依赖包，由 Runner 把它们合并进**同一次**构建调用。
        /// <summary>必须与本包在**同一次 <c>BuildAssetBundles</c> 调用**里构建的其它全局包（本包资源引用了它们的资源时填写；无依赖返回空集合）。</summary>
        IEnumerable<IGlobalBundleProvider> BuildWith { get; }

        // WHY: 下面两条是三个全局包之间仅存的差异（条目表之外）：CameraBg 的条目表固定两条、历史上不清理配置目录，且素材校验遇第一条问题即中止；RoomOne / UI 需要清理残留配置、并把全部问题一次报出。没有它们就只能靠改 Runner 的分支来保行为，等于把差异又抄回流程里。
        /// <summary>是否先删掉配置目录里不属于本次产出的旧配置（残留清理；RoomOne / UI = true，CameraBg = false 以保持其既有语义）。</summary>
        bool CleanStaleConfigs { get; }

        /// <summary>素材校验遇到第一条问题即中止并只报该条（CameraBg = true，沿用旧口径）；false = 收集全部问题、逐条记日志后弹前 6 条（RoomOne / UI）。</summary>
        bool AbortOnFirstAssetProblem { get; }
    }
}
