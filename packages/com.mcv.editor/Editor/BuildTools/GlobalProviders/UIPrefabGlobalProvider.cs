using System;
using System.Collections.Generic;
using System.IO;
using MCV_Module.EditorTools.Common;
using MCV_Module.Interfaces;
using MCV_Module.Models.Addressable;
using UnityEditor;
using UnityEngine;

// WHY: B1.5 的第三个全局包 —— 面板 + 碎片 prefab 打成一包；流程（编译守卫 / 校验 / 写配置 / 同步清单 / 构建）全在 Assets/Editor/Common/GlobalBundleRunner，本文件只留「条目从哪来 + 叫什么 + 菜单入口」。
// WHY: 条目一律扫描目录产出，**不写死 40 条路径** —— 文件名 = 面板类名、id = ui_{文件名}（ContentNaming.UIPrefabId），新增 / 改名 / 删除 prefab 后重跑菜单即可，不存在第二张需要手工同步的映射表；代价是目录不存在时的报错要可读（见 CollectDir 的告警）。
// WHY: 40 个 prefab 进同一个 bundle（不按面板拆包）—— 面板是「按 id 取件」的懒加载契约，拆包只会让取件多一层 bundle 归属判断。
/// <summary>UI 全局包 provider：<c>Assets/Prefabs/UI/Panels</c> + <c>Assets/Prefabs/UI/Fragments</c> 下的 prefab 打成一包（bundle <c>UI/ui</c>）。</summary>
public sealed class UIPrefabGlobalProvider : IGlobalBundleProvider
{
    /// <summary>对账报告里的来源名与日志 tag（日志 tag = <c>[{Name}AB]</c> = <c>[UIAB]</c>）。</summary>
    public string Name => "UI";

    public string MenuPath => "MCV Build/UI prefab AB";

    public string ConfigDir => EditorPaths.UIConfigDir;

    // WHY: 末段必须小写 —— 运行时 GlobalAddressableMgr.GetBundleUrl 会把文件名段强制小写，大小写不一致会在 WebGL / Linux 上 404
    public string BundleName => ContentNaming.UIBundleName;

    // WHY: 目录扫描型包必须开残留清理 —— prefab 改名 / 删除后旧配置没人取，AutoCollect 会把它收回主清单，运行时就多出「配置在、包不在」的可解析 id
    public bool CleanStaleConfigs => true;

    // WHY: 目录扫描一次就能报出全部问题（缺 prefab / id 重复），逐条修比重跑多次菜单快，故不用「遇第一条即中止」
    public bool AbortOnFirstAssetProblem => false;

    /// <summary>面板 prefab 先、碎片 prefab 后（顺序只影响日志与包内顺序，不影响按 id 取件）。</summary>
    public IEnumerable<GlobalResourceEntry> Collect()
    {
        var list = new List<GlobalResourceEntry>();
        CollectDir(EditorPaths.UIPanelPrefabDir, "面板", list);
        CollectDir(EditorPaths.UIFragmentPrefabDir, "碎片", list);
        return list;
    }

    /// <summary>扫描一个目录的**直接子级** prefab；目录不存在 / 为空只告警并跳过（prefab 搬迁未完成时属正常），条目为空由 Runner 统一报 Error 中止。</summary>
    static void CollectDir(string dir, string label, List<GlobalResourceEntry> list)
    {
        if (!AssetDatabase.IsValidFolder(dir))
        {
            Debug.LogWarning($"[UIAB] {label} prefab 目录不存在，已跳过：{dir}（目录建好并导入后再跑本菜单）");
            return;
        }

        var paths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { dir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // WHY: 只取直接子级 —— id 由文件名推导，子目录里的同名 prefab 会撞 id（目录按契约是平铺的：Panels 28 个 / Fragments 12 个）
            if (!string.IsNullOrEmpty(path) && IsDirectChild(path, dir)) paths.Add(path);
        }

        if (paths.Count == 0)
        {
            Debug.LogWarning($"[UIAB] {label} prefab 目录里没有 prefab，已跳过：{dir}");
            return;
        }

        // WHY: FindAssets 的顺序不保证稳定，排序让每次收集的顺序一致（日志与包内顺序可复现）
        paths.Sort(StringComparer.OrdinalIgnoreCase);

        foreach (string path in paths)
            list.Add(new GlobalResourceEntry(ContentNaming.UIPrefabId(Path.GetFileNameWithoutExtension(path)),
                                             path, null, ContentAssetKind.Prefab));

        Debug.Log($"[UIAB] 已收集{label} prefab {paths.Count} 个：{dir}");
    }

    // WHY: 40 个面板 / 碎片 prefab（及其嵌套基座 prefab）都引用了 Assets/Fonts/SIMHEI.TTF，故字体包必须与 UI 包**同批**构建。
    // 实测：分开构建时字体被**复制**进 UI 包（面板包 6187 KB），同一次 BuildAssetBundles 里同时声明两包才只记依赖（面板 152 KB + 字体包 6033 KB）——
    // Unity 的「同一资产被显式分配后不再复制」只在同一次构建调用内成立，而 Runner 是一个 provider 建一个包。
    /// <summary>必须同批构建：字体包（面板 / 碎片 / 基座 prefab 直引 SIMHEI.TTF）。</summary>
    public IEnumerable<IGlobalBundleProvider> BuildWith => new[] { (IGlobalBundleProvider)new FontGlobalProvider() };

    /// <summary>判断 <paramref name="path"/> 是否 <paramref name="dir"/> 的直接子级（AssetDatabase 路径统一按正斜杠、大小写宽松比对）。</summary>
    static bool IsDirectChild(string path, string dir)
    {
        string parent = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parent)) return false;

        return parent.Replace('\\', '/').TrimEnd('/')
                     .Equals(dir.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }

    #region 菜单
    // WHY: MenuItem 需要编译期的常量字符串（不能用 MenuPath 属性），故菜单入口留在本文件；它与 MenuPath 同源，改菜单文字时两处一起改。
    [MenuItem("MCV Build/UI prefab AB", false, 65)]
    public static void Build()
    {
        // WHY: 与 CameraBg / RoomOne 的菜单同形态（构建前弹确认框）；自动化 / MCP 会话请直接调 GlobalBundleRunner.Run(new UIPrefabGlobalProvider(), build, interactive: false)，那条路径不弹任何窗。
        GlobalBundleRunner.Run(new UIPrefabGlobalProvider(), build: true, interactive: true);
    }
    #endregion
}
