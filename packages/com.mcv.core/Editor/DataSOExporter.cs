using System.Collections.Generic;
using System.IO;
using MCV_Module.Models;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/*
    数据 SO → 导出产物 的**唯一门面**（§7.2 L-1 / L-2 / L-3）。

    口径（§7.2）：`*DataSO.asset` 是**唯一真源**（人维护、Inspector 可编辑、随工程版本控制）；
    导出产物（默认 JSON）是**构建期产物**，运行期只读，**不再是手改入口**。

    L-1 导出入口收敛：菜单「初始化 JSON」与构建前钩子**调同一个 ExportAll()**，避免两条路径产出不一致。
    L-2 可审计：导出打印逐 SO 变更摘要，并提供「对账（dry-run）」只比对不写盘。
    L-3 资产有效：创建缺失 SO 前校验脚本就绪、创建后回读自检；同名但脚本丢失的坏资产先删再建。
*/
public static class DataSOExporter
{
    /// <summary>数据 SO 目录（资产落点）。</summary>
    const string DataDir = "Assets/Data/ScriptableObjects";

    #region L-1 唯一导出入口
    /// <summary>
    /// 扫描项目内所有数据 SO（<see cref="DataSO"/> = 实现 <c>IDataExporter</c>）并逐个导出。
    /// **本方法是唯一导出门面**：菜单与构建前钩子都调它。
    /// 已一致（<see cref="DataSO.DryRun"/> 为 null）的 SO **不写盘** —— 避免无意义的时间戳/版本 diff。
    /// </summary>
    public static void ExportAll()
    {
        List<DataSO> exporters = CollectDataSOs();

        int updated = 0, unchanged = 0, failed = 0;
        foreach (DataSO so in exporters)
        {
            string before = so.DryRun();
            if (before == null) { unchanged++; continue; }

            so.Export();

            string after = so.DryRun();
            if (after == null)
            {
                updated++;
                Debug.Log($"[DataSOExporter] ✔ 已更新 {so.GetType().Name}：{before}");
            }
            else
            {
                failed++;
                Debug.LogError($"[DataSOExporter] ✘ {so.GetType().Name} 导出后仍未能与介质一致：{after}");
            }
        }

        Debug.Log($"[DataSOExporter] 导出完成：更新 {updated} / 一致跳过 {unchanged} / 失败 {failed}（共 {exporters.Count} 个数据 SO）");
    }

    /// <summary>手动一键初始化：从所有数据 SO 全量导出（与构建钩子同一条路径）。</summary>
    [MenuItem("MCV/数据/初始化 JSON（从 SO 全量导出）")]
    public static void InitFromSO()
    {
        ExportAll();
    }

    /// <summary>
    /// 只读对账（**绝不写盘**）：逐 SO 比对 SO 数据与介质内容，列出不一致项（§7.2 L-2）。
    /// </summary>
    [MenuItem("MCV/数据/对账 JSON（dry-run，不写盘）")]
    public static void DryRunOnly()
    {
        List<DataSO> exporters = CollectDataSOs();
        var diffs = new List<string>();

        foreach (DataSO so in exporters)
        {
            string diff = so.DryRun();
            if (diff != null) diffs.Add($"   {diff}");
        }

        if (diffs.Count == 0)
        {
            Debug.Log($"[DataSOExporter] 对账通过：{exporters.Count} 个数据 SO 与介质内容一致");
            EditorUtility.DisplayDialog("数据对账（dry-run）",
                $"✔ 对账通过，未写盘\n\n{exporters.Count} 个数据 SO 与导出产物一致。", "确定");
            return;
        }

        Debug.LogWarning($"[DataSOExporter] 对账发现 {diffs.Count} 项差异（未写盘）：\n{string.Join("\n", diffs)}");
        EditorUtility.DisplayDialog("数据对账（dry-run）",
            $"⚠ 发现 {diffs.Count} 项差异（**未写盘**）：\n\n" +
            HeadLines(diffs, 10) + "\n\n完整清单见 Console。", "确定");
    }
    #endregion

    #region L-3 资产创建（带脚本就绪校验）
    /// <summary>一键创建缺失的数据 SO（Assets/Data/ScriptableObjects/）。</summary>
    [MenuItem("MCV/数据/创建缺失的数据 SO")]
    public static void EnsureDataSOs()
    {
        EnsureSO<SystemDataSO>();
        EnsureSO<ProjectDataSO>();
        EnsureSO<UserDataSO>();
        EnsureSO<LanguageDataSO>();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DataSOExporter] 数据 SO 检查完成（缺失的已创建）。");
    }

    /// <summary>
    /// 确保某个数据 SO 存在且**脚本指向有效**。
    ///
    /// L-3：历史坑是「类没写在同名 .cs 里」或「脚本未编译完就 CreateAsset」→ 资产写出
    /// <c>m_Script: {fileID: 0}</c>，加载为 null（表现为「资产在、却读不出数据」）。
    /// 因此这里三道闸：① 脚本就绪探测；② 同名坏资产先删再建；③ 建后回读自检。
    /// </summary>
    static void EnsureSO<T>() where T : ScriptableObject
    {
        string path = $"{DataDir}/{typeof(T).Name}.asset";

        // 已有且能正常解析 → 直接返回（不动它）
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;

        // ① 脚本就绪探测：找不到该类的 MonoScript 说明还没编译完，此时 CreateAsset 必产坏资产
        string[] scripts = AssetDatabase.FindAssets($"{typeof(T).Name} t:MonoScript");
        if (scripts == null || scripts.Length == 0)
        {
            Debug.LogError($"[DataSOExporter] 找不到 {typeof(T).Name} 的 MonoScript（编译未就绪？），已跳过创建。等编译结束再跑本菜单。");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(DataDir))
            AssetDatabase.CreateFolder("Assets/Data", "ScriptableObjects");

        // ② 同名但加载为 null = 脚本丢失的坏资产：先删掉，否则 CreateAsset 会失败
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
            Debug.LogWarning($"[DataSOExporter] 已删除脚本丢失（m_Script 无效）的旧资产并重建：{path}");
        }

        var so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, path);

        // ③ 回读自检：仍为 null 说明脚本没真正就绪，提示重跑而不是留下坏资产
        if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
            Debug.LogError($"[DataSOExporter] {path} 创建后仍无法解析（脚本未编译完成），请重跑本菜单。");
        else
            Debug.Log($"[DataSOExporter] 已创建数据 SO：{path}");
    }
    #endregion

    #region 私有工具
    static List<DataSO> CollectDataSOs()
    {
        var list = new List<DataSO>();
        foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so is DataSO dataSo) list.Add(dataSo);      // DataSO 实现 IDataExporter，扫描口径不变
        }
        return list;
    }

    static string HeadLines(List<string> lines, int max)
    {
        if (lines == null || lines.Count == 0) return "";
        if (lines.Count <= max) return string.Join("\n", lines);
        return string.Join("\n", lines.GetRange(0, max)) + $"\n…… 另有 {lines.Count - max} 条，见 Console";
    }
    #endregion
}

/// <summary>构建前自动初始化：从所有数据 SO 全量覆盖到导出产物，保证打包内为最新（与菜单同一入口）。</summary>
public class DataSOBuildInit : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        DataSOExporter.ExportAll();
    }
}
