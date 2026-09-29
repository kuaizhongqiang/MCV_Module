using System.Collections.Generic;
using System.IO;
using System.Text;
using MCV_Module.EditorTools.Common;
using MCV_Module.Models;
using MCV_Module.Models.System;
using MCV_Module.UI.Components;
using MCV_Module.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Legacy Text → TextComponent 的半自动迁移：扫描 prefab、把 `m_FontData.*` 与 `m_Color` 搬进组件字段，并输出可逐条核对的报告。
/// 幂等（已有 TextComponent 的节点跳过）；**不销毁 Text**（保留 Legacy 才有所见即所得）；实例覆盖转置是另一个菜单的事。
/// </summary>
public static class TextMigrationTools
{
    const string ReportDirName = "TextMigration";
    const string MenuRoot = "MCV Editor/文字/";

    [MenuItem(MenuRoot + "迁移 dry-run（只报告）", false, 70)]
    public static void DryRun() => Run(false);

    [MenuItem(MenuRoot + "迁移（落盘，先备份报告）", false, 71)]
    public static void Migrate()
    {
        bool ok = EditorUtility.DisplayDialog("迁移 Text → TextComponent",
            "会给每个 Legacy Text 节点补挂 TextComponent 并把字体 id / 字号 / 字重 / 颜色 / 对齐 / 文本搬进组件字段。\n\n"
            + "不会删除 Text 组件，不动 prefab 实例覆盖。落盘前会先写一份报告到 Temp/MCV_TextMigration/。",
            "开始迁移", "取消");
        if (!ok) return;
        Run(true);
    }

    // ── 执行 ────────────────────────────────────────────────────────────────
    /// <summary>执行迁移；`apply=false` 只出报告。公开是为了能被批处理 / MCP 驱动（不弹模态框）。</summary>
    public static void Run(bool apply)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        var rows = new List<Row>();
        var skippedInputField = new List<Row>();
        var dropdownBound = new List<Row>();
        int scanned = 0, already = 0, changedFiles = 0, nested = 0, repaired = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;
                if (path.Contains("/Plugins/") || path.StartsWith("Packages/")) continue;
                if (!path.EndsWith(".prefab")) continue;

                bool progress = EditorUtility.DisplayCancelableProgressBar(
                    "扫描 prefab", path, (float)i / Mathf.Max(1, guids.Length));
                if (progress)
                {
                    Log.Warning("[TextMigration] 用户取消，已处理的文件保持原样");
                    break;
                }

                scanned++;
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                bool dirty = false;
                try
                {
                    Text[] texts = contents.GetComponentsInChildren<Text>(true);
                    for (int t = 0; t < texts.Length; t++)
                    {
                        Text text = texts[t];

                        // WHY: GetComponentsInChildren(true) 会把**嵌套 prefab 实例**里的 Text 也带出来（同一个 GeneralBtn 在
                        // MenuPanel 里被重复计数），往那种节点加组件还会给宿主预制体塞进一条实例覆盖。嵌套节点属于它自己的
                        // 预制体资产，处理那个资产时自然会轮到，这里必须跳过。
                        GameObject instRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(text.gameObject);
                        if (instRoot != null && instRoot != contents) { nested++; continue; }

                        var row = Read(text, path);

                        TextComponent existing = text.GetComponent<TextComponent>();
                        if (existing != null)
                        {
                            already++;
                            // WHY: 补齐路径 —— 早期版本把 m_Text 读成了 m_FontData.m_Text，落盘时文案是空的；重跑时按源值补回，仍然幂等。
                            if (apply && string.IsNullOrEmpty(ReadComponentText(existing)) && !string.IsNullOrEmpty(row.text))
                            {
                                var fix = new SerializedObject(existing);
                                fix.FindProperty("text").stringValue = row.text;
                                fix.ApplyModifiedPropertiesWithoutUndo();
                                repaired++;
                                dirty = true;
                            }
                            continue;
                        }

                        if (IsInputFieldPart(text))
                        {
                            // WHY: InputField 的 m_TextComponent / m_Placeholder 是输入控件状态，不是显示文案，不走本组件。
                            row.flag = "例外：InputField";
                            skippedInputField.Add(row);
                            continue;
                        }
                        if (IsDropdownPart(text)) { row.flag = "注意：Dropdown 绑定（B4 不得换形态）"; dropdownBound.Add(row); }

                        rows.Add(row);
                        if (!apply) continue;

                        Apply(contents, text, row);
                        dirty = true;
                    }
                }
                finally
                {
                    if (dirty) { PrefabUtility.SaveAsPrefabAsset(contents, path); changedFiles++; }
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        string report = WriteReport(scanned, rows, skippedInputField, dropdownBound, already, changedFiles, nested, repaired, apply);
        AssetDatabase.Refresh();
        // WHY: 收尾只用非模态日志 —— 模态框会卡住 MCP 会话（旧分支踩过：mcpforunity 的调用被 DisplayDialog 挂死）。
        Log.Info($"[TextMigration] {(apply ? "已迁移" : "dry-run")}：扫描 {scanned} 个 prefab，命中 {rows.Count} 个节点，"
                 + $"已迁移跳过 {already}，InputField 例外 {skippedInputField.Count}，Dropdown 绑定 {dropdownBound.Count}，"
                 + $"嵌套实例节点跳过 {nested}，补齐文案 {repaired}，落盘文件 {changedFiles}。报告：{report}");
    }

    // ── 读源数据 ────────────────────────────────────────────────────────────
    class Row
    {
        public string asset, nodePath, componentId, fontLabel, fontId;
        public int fontSize, fontStyle, alignment;
        public Color color;
        public string text, flag = "";
    }

    static Row Read(Text text, string assetPath)
    {
        var so = new SerializedObject(text);
        var fontProp = so.FindProperty("m_FontData.m_Font");
        Font font = fontProp != null ? fontProp.objectReferenceValue as Font : null;

        var row = new Row
        {
            asset = assetPath,
            nodePath = HierarchyPath(text.transform),
            componentId = ObjectId(text),
            fontSize = so.FindProperty("m_FontData.m_FontSize")?.intValue ?? 0,
            fontStyle = so.FindProperty("m_FontData.m_FontStyle")?.enumValueIndex ?? 0,
            alignment = so.FindProperty("m_FontData.m_Alignment")?.intValue ?? 0,
            color = so.FindProperty("m_Color")?.colorValue ?? Color.white,
            // WHY: `m_Text` 是 Text 的**直接字段**（与 m_FontData 平级），不在 FontData 结构里；只有字体相关属性才在 m_FontData 下。
            text = so.FindProperty("m_Text")?.stringValue ?? string.Empty,
        };
        row.fontId = ResolveFontId(font, out row.fontLabel);
        return row;
    }

    /// <summary>字体 → fontId。SIMHEI 与 Unity 内建 Arial 都归 `ui`（Arial 换 SIMHEI 已定），LCD/数字字体归 `digital`。</summary>
    static string ResolveFontId(Font font, out string label)
    {
        if (font == null) { label = "(null)"; return "ui"; }
        string assetPath = AssetDatabase.GetAssetPath(font);
        label = string.IsNullOrEmpty(assetPath) ? font.name + "（内建）" : assetPath;
        string n = font.name.ToLowerInvariant();
        if (n.Contains("digital") || n.Contains("lcd")) return "digital";
        return "ui";
    }

    // ── 写组件 ──────────────────────────────────────────────────────────────
    static void Apply(GameObject contents, Text text, Row row)
    {
        var comp = text.gameObject.AddComponent<TextComponent>();
        var so = new SerializedObject(comp);
        so.FindProperty("fontId").stringValue = row.fontId;
        so.FindProperty("text").stringValue = row.text;
        so.FindProperty("languageKey").stringValue = string.Empty;
        so.FindProperty("fontSize").intValue = row.fontSize;
        so.FindProperty("fontStyle").enumValueIndex = Mathf.Clamp(row.fontStyle, 0, 3);
        so.FindProperty("color").colorValue = row.color;
        so.FindProperty("alignment").enumValueIndex = ToOverrideAlignment(row.alignment);
        so.FindProperty("cjkTypography").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        // WHY: 内建 Arial 的节点已定"换成 ui(SIMHEI)"，但换的是**资产引用**；字体清单还没建时先记下来，等 B3 对齐。
        if (row.fontLabel != null && row.fontLabel.Contains("内建"))
        {
            string path = FontCatalog.LegacyFontPathOf(row.fontId);
            Font target = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Font>(path);
            if (target != null) text.font = target;
            else row.flag = Append(row.flag, "字体待 B3 对齐（FontCatalog 未登记）");
        }
        if (!string.IsNullOrEmpty(text.text) && string.IsNullOrEmpty(row.text))
            row.flag = Append(row.flag, "注意：Text 文本为空但字段非空");
    }

    /// <summary>TextAnchor 0–8 与 OverrideAlignment 前 9 项同序；9 = Justified；其余按 Auto。</summary>
    static int ToOverrideAlignment(int textAnchor)
    {
        if (textAnchor >= 0 && textAnchor <= 8) return textAnchor;
        if (textAnchor == 9) return (int)OverrideAlignment.Justified;
        return (int)OverrideAlignment.Auto;
    }

    // ── 辅助 ────────────────────────────────────────────────────────────────
    static bool IsInputFieldPart(Text text)
    {
        InputField field = text.GetComponentInParent<InputField>(true);
        return field != null && (field.textComponent == text || field.placeholder == text);
    }

    static bool IsDropdownPart(Text text)
    {
        Dropdown dd = text.GetComponentInParent<Dropdown>(true);
        return dd != null && (dd.captionText == text || dd.itemText == text);
    }

    static string ReadComponentText(TextComponent comp)
    {
        var so = new SerializedObject(comp);
        return so.FindProperty("text")?.stringValue ?? string.Empty;
    }

    static string Append(string a, string b) => string.IsNullOrEmpty(a) ? b : a + "；" + b;

    static string HierarchyPath(Transform t)
    {
        var sb = new StringBuilder(t.name);
        Transform p = t.parent;
        while (p != null) { sb.Insert(0, p.name + "/"); p = p.parent; }
        return sb.ToString();
    }

    static string ObjectId(Object o)
    {
        return GlobalObjectId.GetGlobalObjectIdSlow(o).targetObjectId.ToString();
    }

    // ── 报告 ────────────────────────────────────────────────────────────────
    static string WriteReport(int scanned, List<Row> rows, List<Row> inputField, List<Row> dropdown,
                              int already, int changedFiles, int nested, int repaired, bool apply)
    {
        string dir = EditorPaths.TempRoot(ReportDirName);
        Directory.CreateDirectory(dir);

        var csv = new StringBuilder("asset,nodePath,componentId,fontAsset,fontId,fontSize,fontStyle,alignment,color,text,flag\n");
        foreach (Row r in rows)
        {
            csv.AppendLine(string.Join(",",
                Csv(r.asset), Csv(r.nodePath), r.componentId, Csv(r.fontLabel), r.fontId,
                r.fontSize, r.fontStyle, r.alignment, ColorUtility.ToHtmlStringRGBA(r.color),
                Csv(Trim(r.text, 24)), Csv(r.flag)));
        }
        foreach (Row r in inputField)
            csv.AppendLine(string.Join(",", Csv(r.asset), Csv(r.nodePath), r.componentId, Csv(r.fontLabel),
                r.fontId, r.fontSize, r.fontStyle, r.alignment, ColorUtility.ToHtmlStringRGBA(r.color), Csv(Trim(r.text, 24)), Csv(r.flag)));

        string csvPath = Path.Combine(dir, "nodes.csv");
        File.WriteAllText(csvPath, csv.ToString(), new UTF8Encoding(false));

        var md = new StringBuilder();
        md.AppendLine("# Text → TextComponent 迁移报告（" + (apply ? "已落盘" : "dry-run") + "）");
        md.AppendLine();
        md.AppendLine("- 扫描 prefab：" + scanned);
        md.AppendLine("- 命中节点：" + rows.Count);
        md.AppendLine("- 已迁移跳过（幂等）：" + already);
        md.AppendLine("- 嵌套 prefab 实例节点跳过：" + nested);
        md.AppendLine("- 已存在组件、补齐文案：" + repaired);
        md.AppendLine("- InputField 例外：" + inputField.Count);
        md.AppendLine("- 落盘文件：" + changedFiles);
        md.AppendLine("- Dropdown 绑定节点（B4 不得换形态）：" + dropdown.Count);
        md.AppendLine();
        md.AppendLine("逐节点明细见 `nodes.csv`。");
        string mdPath = Path.Combine(dir, "report.md");
        File.WriteAllText(mdPath, md.ToString(), new UTF8Encoding(false));
        return mdPath;
    }

    static string Csv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("\"", "\"\"");
        return "\"" + s + "\"";
    }

    static string Trim(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("\n", "\\n").Replace("\r", "");
        return s.Length <= n ? s : s.Substring(0, n) + "…";
    }
}
