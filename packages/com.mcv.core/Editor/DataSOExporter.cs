using MCV_Module.Models;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// WHY: 约定 —— 编辑/创建走 SO，运行走 JSON；初始化 = 从所有数据 SO 全量覆盖到 JSON，触发方式为手动一键菜单或构建前自动执行。
/// <summary>数据 SO → JSON 初始化工具。</summary>
public static class DataSOExporter
{
    /// <summary>扫描项目内所有数据 SO（实现 IDataExporter）并逐个导出到 JSON。</summary>
    public static void ExportAll()
    {
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so is IDataExporter exporter)
            {
                exporter.Export();
                count++;
            }
        }
        Debug.Log($"[DataSOExporter] 初始化完成：共导出 {count} 个数据 SO → JSON");
    }

    /// <summary>手动一键初始化：从所有数据 SO 全量覆盖到 JSON。</summary>
    [MenuItem("MCV Editor/数据/初始化 JSON（从 SO 全量导出）")]
    public static void InitFromSO()
    {
        ExportAll();
    }

    /// <summary>创建缺失的数据 SO（Assets/Data/ScriptableObjects/）。危险项：空 SO 会被构建前钩子覆盖 JSON，故先弹确认（AGENT.md 红线）。</summary>
    [MenuItem("MCV Editor/危险/创建缺失的数据 SO")]
    public static void EnsureDataSOs()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "危险操作：创建缺失的数据 SO",
            "本操作会为缺失的数据类型创建「空 SO」，而构建前钩子会用空 SO 覆盖 StreamingAssets/Data/*.json。\n\n"
            + "只有在你已备份 JSON、或本来就打算重建数据时才继续。正常出包不需要跑它。",
            "我已备份，继续",
            "取消");
        if (!confirmed) return;

        EnsureSO<SystemDataSO>();
        EnsureSO<ProjectDataSO>();
        EnsureSO<UserDataSO>();
        EnsureSO<LanguageDataSO>();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DataSOExporter] 数据 SO 检查完成（缺失的已创建）。");
    }

    static void EnsureSO<T>() where T : ScriptableObject
    {
        if (AssetDatabase.FindAssets($"t:{typeof(T).Name}").Length > 0) return;
        const string dir = "Assets/Data/ScriptableObjects";
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Data", "ScriptableObjects");
        var so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, $"{dir}/{typeof(T).Name}.asset");
    }
}

/// <summary>构建前自动初始化：从所有数据 SO 全量覆盖到 JSON，保证打包内 JSON 为最新。</summary>
public class DataSOBuildInit : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        DataSOExporter.ExportAll();
    }
}
