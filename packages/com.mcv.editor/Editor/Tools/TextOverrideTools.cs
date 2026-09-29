using System.Collections.Generic;
using System.IO;
using System.Text;
using MCV_Module.EditorTools.Common;
using MCV_Module.UI.Components;
using MCV_Module.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 把 prefab 实例上针对 Legacy Text 的覆盖**转置**到 TextComponent 上：`m_Text` → `text`、`m_FontData.m_FontSize` → `fontSize`、
/// `m_FontData.m_FontStyle` → `fontStyle`。
/// 为什么必须转置：迁移后 TextComponent.Awake 会用组件里的基值写 `Text.text`，实例覆盖的 `m_Text` 会被它盖掉（房间 HUD 名、器件名等就是这样丢的）。
/// 悬空覆盖（target 已失效，TMP 时代遗留）**不转置**，只进报告。
/// </summary>
public static class TextOverrideTools
{
    const string MenuRoot = "MCV Editor/文字/";
    const string ReportDirName = "TextOverride";

    [MenuItem(MenuRoot + "转置实例覆盖 dry-run（只报告）", false, 72)]
    public static void DryRunOverrides() => Run(false);

    [MenuItem(MenuRoot + "转置实例覆盖（落盘）", false, 73)]
    public static void ApplyOverrides()
    {
        bool ok = EditorUtility.DisplayDialog("转置实例覆盖",
            "把 prefab 实例上对 Legacy Text 的 m_Text / m_FontData.m_FontSize / m_FontData.m_FontStyle 覆盖，"
            + "同步写成对 TextComponent 的 text / fontSize / fontStyle 覆盖。\n\n悬空覆盖（目标已失效）只进报告、不改。",
            "开始转置", "取消");
        if (!ok) return;
        Run(true);
    }

    /// <summary>执行转置；`apply=false` 只出报告。公开以便 MCP / 批处理驱动。</summary>
    public static void Run(bool apply)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        var transposed = new List<string>();
        var dangling = new List<string>();
        var skipped = new List<string>();
        int scanned = 0, changedFiles = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) continue;
                if (path.Contains("/Plugins/") || path.StartsWith("Packages/")) continue;

                if (EditorUtility.DisplayCancelableProgressBar("扫描实例覆盖", path, (float)i / Mathf.Max(1, guids.Length)))
                {
                    Log.Warning("[TextOverride] 用户取消，已处理的文件保持原样");
                    break;
                }

                scanned++;
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                bool dirty = false;
                try
                {
                    foreach (GameObject inst in OutermostInstanceRoots(contents))
                    {
                        PropertyModification[] mods = PrefabUtility.GetPropertyModifications(inst);
                        if (mods == null || mods.Length == 0) continue;

                        var next = new List<PropertyModification>(mods);
                        bool changed = false;

                        for (int m = 0; m < mods.Length; m++)
                        {
                            PropertyModification mod = mods[m];
                            if (mod.target == null)
                            {
                                // WHY: 目标 fileID 在基座里已不存在（TMP 时代遗留的 m_text / m_fontSize / m_fontSizeBase）。
                                dangling.Add($"{path} | {inst.name} | {mod.propertyPath} = {mod.value}");
                                continue;
                            }

                            string target = MapProperty(mod.propertyPath, out string componentField);
                            if (target == null) continue;

                            var text = mod.target as Text;
                            if (text == null) { skipped.Add($"{path} | {inst.name} | {mod.propertyPath}（目标不是 Text）"); continue; }

                            var comp = text.GetComponent<TextComponent>();
                            if (comp == null) { skipped.Add($"{path} | {inst.name} | {mod.propertyPath}（该节点还没有 TextComponent）"); continue; }

                            if (HasModification(next, comp, componentField)) continue; // 幂等：已转置过
                            next.Add(new PropertyModification { target = comp, propertyPath = componentField, value = mod.value });
                            transposed.Add($"{path} | {inst.name} | {text.gameObject.name} | {mod.propertyPath} → {componentField} = {mod.value}");
                            changed = true;
                        }

                        if (changed)
                        {
                            dirty = true;
                            if (apply) PrefabUtility.SetPropertyModifications(inst, next.ToArray());
                        }
                    }
                }
                finally
                {
                    if (dirty && apply) { PrefabUtility.SaveAsPrefabAsset(contents, path); changedFiles++; }
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        string report = WriteReport(scanned, transposed, dangling, skipped, changedFiles, apply);
        AssetDatabase.Refresh();
        // WHY: 只用非模态日志 —— 模态框会卡住 MCP 会话。
        Log.Info($"[TextOverride] {(apply ? "已转置" : "dry-run")}：扫描 {scanned} 个 prefab，"
                 + $"转置 {transposed.Count} 条，悬空 {dangling.Count} 条，跳过 {skipped.Count} 条，落盘 {changedFiles} 个文件。报告：{report}");
    }

    /// <summary>Legacy 侧属性路径 → TextComponent 侧字段名；不关心的返回 null。</summary>
    static string MapProperty(string propertyPath, out string componentField)
    {
        switch (propertyPath)
        {
            case "m_Text": componentField = "text"; return componentField;
            case "m_FontData.m_FontSize": componentField = "fontSize"; return componentField;
            case "m_FontData.m_FontStyle": componentField = "fontStyle"; return componentField;
            default: componentField = null; return null;
        }
    }

    static bool HasModification(List<PropertyModification> mods, Object target, string propertyPath)
    {
        for (int i = 0; i < mods.Count; i++)
        {
            if (mods[i].target == target && mods[i].propertyPath == propertyPath) return true;
        }
        return false;
    }

    /// <summary>取所有「最外层是嵌套 prefab 实例」的根（不含被扫描的 prefab 自身）。</summary>
    static IEnumerable<GameObject> OutermostInstanceRoots(GameObject contents)
    {
        var seen = new HashSet<GameObject>();
        Transform[] all = contents.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(all[i].gameObject);
            if (root != null && root != contents && seen.Add(root)) yield return root;
        }
    }

    static string WriteReport(int scanned, List<string> transposed, List<string> dangling, List<string> skipped,
                              int changedFiles, bool apply)
    {
        string dir = EditorPaths.TempRoot(ReportDirName);
        Directory.CreateDirectory(dir);
        var sb = new StringBuilder();
        sb.AppendLine("# 实例覆盖转置报告（" + (apply ? "已落盘" : "dry-run") + "）");
        sb.AppendLine();
        sb.AppendLine("- 扫描 prefab：" + scanned);
        sb.AppendLine("- 转置：" + transposed.Count);
        sb.AppendLine("- 悬空（目标已失效，只报告）：" + dangling.Count);
        sb.AppendLine("- 跳过：" + skipped.Count);
        sb.AppendLine("- 落盘文件：" + changedFiles);
        sb.AppendLine();
        Append(sb, "转置明细", transposed);
        Append(sb, "悬空覆盖（待清理，勿转置）", dangling);
        Append(sb, "跳过", skipped);
        string path = Path.Combine(dir, "report.md");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        return path;
    }

    static void Append(StringBuilder sb, string title, List<string> lines)
    {
        sb.AppendLine("## " + title);
        if (lines.Count == 0) { sb.AppendLine("（无）"); sb.AppendLine(); return; }
        for (int i = 0; i < lines.Count; i++) sb.AppendLine("- " + lines[i]);
        sb.AppendLine();
    }
}
