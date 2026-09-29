using System.Collections.Generic;
using System.IO;
using System.Text;
using MCV_Module.EditorTools.Common;
using MCV_Module.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 业务数据的英文列补齐：在 <c>StreamingAssets/Data/*.json</c> 里给已登记的字段补出对应的 <c>*En</c> 键（缺则补空串，已填的不动）。
/// 为什么用 JObject 而不是反序列化再写回：反序列化会丢掉模型里没有的键（例如题库里那份历史遗留的 questions[]），
/// 逐键增补能保证**只加不改**。翻译就填这些空串，运行期由 Localized.Pick 按语言取列。
/// </summary>
public static class TextI18nDataTools
{
    const string MenuRoot = "MCV Editor/文字/";
    const string ReportDirName = "TextI18nData";

    /// <summary>中文字段 → 英文列。只覆盖 Models 里真加了 En 字段的那些。</summary>
    static readonly string[,] Pairs =
    {
        { "displayName", "displayNameEn" },
        { "description", "descriptionEn" },
        { "questionText", "questionTextEn" },
        { "itemText", "itemTextEn" },
        { "title", "titleEn" },
        { "contentText", "contentTextEn" },
        { "stepTips", "stepTipsEn" },
        { "opTips", "opTipsEn" },
        { "projectName", "projectNameEn" },
        { "company", "companyEn" },
        { "copyright", "copyrightEn" },
    };

    /// <summary>集合列：zh 数组 → En 数组（等长，空串填充）。</summary>
    static readonly string[,] ListPairs =
    {
        { "pages", "pagesEn" },
    };

    [MenuItem(MenuRoot + "补齐业务数据英文列 dry-run（只报告）", false, 74)]
    public static void DryRun() => Run(false);

    [MenuItem(MenuRoot + "补齐业务数据英文列（落盘）", false, 75)]
    public static void RunApply() => Run(true);

    /// <summary>执行补齐；apply=false 只统计。公开以便 MCP / 批处理驱动。</summary>
    public static void Run(bool apply)
    {
        string dir = Path.Combine(Application.dataPath, "StreamingAssets/Data");
        if (!Directory.Exists(dir)) { Log.Error($"[TextI18nData] 目录不存在：{dir}"); return; }

        var report = new List<string>();
        int files = 0, added = 0, filled = 0;

        foreach (string path in Directory.GetFiles(dir, "*.json"))
        {
            // WHY: LanguageData.json 是 key 表本身，它的 displayName 是开发标签（等于 key），不是业务文案，不该长出英文列。
            if (Path.GetFileName(path) == "LanguageData.json") continue;

            string raw = File.ReadAllText(path, Encoding.UTF8);
            JToken root;
            try { root = JToken.Parse(raw); }
            catch (System.Exception e) { Log.Warning($"[TextI18nData] 跳过（JSON 解析失败）：{Path.GetFileName(path)} {e.Message}"); continue; }

            int fileAdded = 0, fileFilled = 0;
            Walk(root, report, Path.GetFileName(path), ref fileAdded, ref fileFilled);
            if (fileAdded + fileFilled == 0) continue;

            files++;
            added += fileAdded;
            filled += fileFilled;
            if (apply)
            {
                File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented), new UTF8Encoding(false));
                report.Add($"（已写盘）{Path.GetFileName(path)}");
            }
        }

        string reportPath = WriteReport(files, added, filled, report, apply);
        if (apply) AssetDatabase.Refresh();
        Log.Info($"[TextI18nData] {(apply ? "已落盘" : "dry-run")}：涉及 {files} 个文件，新增英文列键 {added} 个，已有英文列的 {filled} 个。报告：{reportPath}");
    }

    static void Walk(JToken token, List<string> report, string file, ref int added, ref int filled)
    {
        if (token is JObject obj)
        {
            for (int p = 0; p < Pairs.GetLength(0); p++)
            {
                string zh = Pairs[p, 0], en = Pairs[p, 1];
                JToken value = obj[zh];
                if (value == null || value.Type != JTokenType.String) continue;
                if (obj[en] != null) { filled++; continue; }
                obj[en] = string.Empty;
                added++;
                report.Add($"{file} :: {obj.Path}.{en}（新增空串）");
            }
            for (int p = 0; p < ListPairs.GetLength(0); p++)
            {
                string zh = ListPairs[p, 0], en = ListPairs[p, 1];
                JToken value = obj[zh];
                if (value == null || value.Type != JTokenType.Array) continue;
                if (obj[en] != null) { filled++; continue; }
                var source = (JArray)value;
                var arr = new JArray();
                for (int i = 0; i < source.Count; i++) arr.Add(string.Empty);
                obj[en] = arr;
                added++;
                report.Add($"{file} :: {obj.Path}.{en}（新增 {arr.Count} 个空串槽）");
            }
            foreach (JProperty prop in new List<JProperty>(obj.Properties())) Walk(prop.Value, report, file, ref added, ref filled);
        }
        else if (token is JArray array)
        {
            foreach (JToken child in array) Walk(child, report, file, ref added, ref filled);
        }
    }

    static string WriteReport(int files, int added, int filled, List<string> lines, bool apply)
    {
        string dir = EditorPaths.TempRoot(ReportDirName);
        Directory.CreateDirectory(dir);
        var sb = new StringBuilder();
        sb.AppendLine("# 业务数据英文列补齐报告（" + (apply ? "已落盘" : "dry-run") + "）");
        sb.AppendLine();
        sb.AppendLine("- 涉及文件：" + files);
        sb.AppendLine("- 新增英文列键：" + added);
        sb.AppendLine("- 已有英文列（跳过）：" + filled);
        sb.AppendLine();
        for (int i = 0; i < lines.Count; i++) sb.AppendLine("- " + lines[i]);
        string path = Path.Combine(dir, "report.md");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        return path;
    }
}
