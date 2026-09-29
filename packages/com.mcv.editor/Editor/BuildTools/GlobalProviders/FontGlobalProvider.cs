using System.Collections.Generic;
using MCV_Module.EditorTools.Common;
using MCV_Module.Interfaces;
using MCV_Module.Models;
using MCV_Module.Models.Addressable;
using UnityEditor;
using UnityEngine;

// WHY: B3 的第四个全局包 —— 字体（Legacy TTF + TMP SDF）打成一包；流程（编译守卫 / 收集 / 校验 / 写配置 / 同步清单 / 构建）全在 Assets/Editor/Common/GlobalBundleRunner，本文件只留「条目从哪来 + 叫什么 + 菜单入口」。
// WHY: 条目来源 = **读 FontCatalogSO**（既不是目录扫描，也不是写死的固定表）—— 加字体只改那份清单、零代码，这是本 provider 的设计要点。
//       清单本身只存路径字符串、不持字体引用，所以它自己不会把 33.8MB 的 TMP 资产拖进 resources.assets（那正是「字体进 AB、default 包不含字体」要避免的事）。
// WHY: 一个 fontId 产出**两条**（legacy / tmp 各一条）—— 两种形态是两份独立资产，运行期也按两条配置各自按键取件（font_ui_legacy / font_ui_tmp）。
/// <summary>字体全局包 provider：<c>FontCatalogSO</c> 里每个 fontId 的 Legacy TTF 与 TMP SDF 各产出一条，打成一包（bundle <c>Fonts/font</c>）。</summary>
public sealed class FontGlobalProvider : IGlobalBundleProvider
{
    /// <summary>对账报告里的来源名与日志 tag（日志 tag = <c>[{Name}AB]</c> = <c>[FontAB]</c>）。</summary>
    public string Name => "Font";

    public string MenuPath => "MCV Build/字体 AB";

    public string ConfigDir => EditorPaths.FontConfigDir;

    // WHY: 末段必须小写 —— 运行时 GlobalAddressableMgr.GetBundleUrl 会把文件名段强制小写，大小写不一致会在 WebGL / Linux 上 404
    public string BundleName => ContentNaming.FontBundleName;

    // WHY: 清单里删掉一条 / fontId 改名后，旧配置没人取；AutoCollect（按类型全量重收）会把它收回主清单，运行时就多出「配置在、包不在」的可解析 id
    public bool CleanStaleConfigs => true;

    // WHY: 一次能报出全部问题（路径为空 / bundle 名不一致 / id 重复），逐条修比重跑多次菜单快，故不用「遇第一条即中止」
    public bool AbortOnFirstAssetProblem => false;

    /// <summary>读 <c>FontCatalogSO</c> 产出条目：每个 fontId 的 legacy / tmp 两个槽位各一条（路径为空的槽位跳过并告警）。</summary>
    public IEnumerable<GlobalResourceEntry> Collect()
    {
        var list = new List<GlobalResourceEntry>();

        FontCatalogSO catalog = LoadCatalog();
        if (catalog == null) return list;   // WHY: 已报 Error，返回空表让 Runner 走「没有收集到任何资源条目」那条统一中止路径（不产出空包、不动任何配置）

        foreach (FontCatalogEntry entry in catalog.Entries)
        {
            if (entry == null) continue;

            if (string.IsNullOrEmpty(entry.id))
            {
                // WHY: id 是运行期取件键（font_{id}_{slot}），为空就拼不出配置 id，Runner 也会把它丢掉 —— 在这里先告警，免得只看到「条目变少」
                Debug.LogWarning("[FontAB] 字体清单里有一条 id 为空的条目，已跳过（id 是运行期取件键，不能为空）");
                continue;
            }

            // WHY: 强校验 bundle 名 —— 清单里写的包名就是运行期 FontCatalog.BundleNameOf(fontId) 用来预加载的那个名字，
            //       必须与 provider 实际写进配置的 bundle 名一致；不一致时运行期按 Fonts/font 预加载会一条都收不到
            //       （GlobalAddressableMgr.GetConfigsByBundleName 是精确字符串匹配），表现为「字体包已构建却始终取不到」。
            if (entry.bundleName != ContentNaming.FontBundleName)
            {
                Debug.LogError($"[FontAB] 字体清单条目「{entry.id}」的 bundleName 是「{entry.bundleName}」，" +
                               $"与字体包名「{ContentNaming.FontBundleName}」不一致，已中止：运行期按 Fonts/font 预加载会取不到任何配置");
                return new List<GlobalResourceEntry>();
            }

            AddSlot(list, entry, ContentNaming.FontSlotLegacy, entry.legacyFontPath, ContentAssetKind.Font);
            AddSlot(list, entry, ContentNaming.FontSlotTmp, entry.tmpFontAssetPath, ContentAssetKind.TmpFont);
        }

        Debug.Log($"[FontAB] 已从 FontCatalogSO 收集字体条目 {list.Count} 条（{catalog.Entries.Count} 个 fontId × 2 种形态，路径为空的槽位已跳过）");
        return list;
    }

    // WHY: 字体是叶子包 —— TTF / SDF 不引用任何别的全局包，故字体菜单单独跑时只建字体包，这是正确的
    // （反过来 UI 包引用了字体，由 UIPrefabGlobalProvider 声明 BuildWith = 字体包）。
    /// <summary>无跨包依赖：字体不引用其它全局包的资源，可单独构建。</summary>
    public IEnumerable<IGlobalBundleProvider> BuildWith => System.Array.Empty<IGlobalBundleProvider>();

    /// <summary>产出一个槽位的条目；路径为空则跳过并告警（单形态缺失不阻塞另一形态进包）。</summary>
    static void AddSlot(List<GlobalResourceEntry> list, FontCatalogEntry entry, string slot, string assetPath,
                        ContentAssetKind kind)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"[FontAB] 字体「{entry.id}」的 {slot} 槽位路径为空，已跳过该条（在 FontCatalogSO 里补上路径后再跑本菜单）");
            return;
        }

        // WHY: assetKind 必须显式写 —— 不写就吃 GlobalResourceEntry 的字段默认值 Prefab，运行期会按 GameObject 泛型加载字体而恒为 null
        list.Add(new GlobalResourceEntry(ContentNaming.FontAssetId(entry.id, slot), assetPath, null, kind));
    }

    /// <summary>取 <c>FontCatalogSO</c>；找不到 / 加载不出来都报 Error 并返回 null（调用方据此返回空表）。</summary>
    static FontCatalogSO LoadCatalog()
    {
        string[] guids = AssetDatabase.FindAssets("t:FontCatalogSO");
        if (guids == null || guids.Length == 0)
        {
            Debug.LogError("[FontAB] 工程里找不到 FontCatalogSO（应为 Assets/Resources/Config/FontCatalog.asset），无法收集字体条目");
            return null;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var catalog = AssetDatabase.LoadAssetAtPath<FontCatalogSO>(path);
        if (catalog == null)
        {
            Debug.LogError($"[FontAB] FontCatalogSO 加载失败（脚本未编译就绪 / 资产损坏）：{path}");
            return null;
        }

        // WHY: 运行期 ResourcePath 是固定字面量 Config/FontCatalog，只认那一份；多出来的清单不会被任何人读到，属于「改了半天没生效」的经典坑
        if (guids.Length > 1)
            Debug.LogWarning($"[FontAB] 工程里有 {guids.Length} 份 FontCatalogSO，只取第一份：{path}（运行期 Resources.Load 只认 Config/FontCatalog 这一份，其余应删除）");

        return catalog;
    }

    #region 菜单
    // WHY: MenuItem 需要编译期的常量字符串（不能用 MenuPath 属性），故菜单入口留在本文件；它与 MenuPath 同源，改菜单文字时两处一起改。
    [MenuItem("MCV Build/字体 AB", false, 66)]
    public static void Build()
    {
        // WHY: 与 CameraBg / RoomOne / UI 的菜单同形态（构建前弹确认框）；自动化 / MCP 会话请直接调 GlobalBundleRunner.Run(new FontGlobalProvider(), build, interactive: false)，那条路径不弹任何窗。
        GlobalBundleRunner.Run(new FontGlobalProvider(), build: true, interactive: true);
    }
    #endregion
}
