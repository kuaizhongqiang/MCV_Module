using MCV_Module.Models;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/*
    数据 SO → JSON 初始化工具。
    约定：编辑/创建走 SO，运行走 JSON；初始化 = 从所有数据 SO 全量覆盖到 JSON。
    触发方式：手动一键菜单，或构建前自动执行。
*/
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
    [MenuItem("MCV/数据/初始化 JSON（从 SO 全量导出）")]
    public static void InitFromSO()
    {
        ExportAll();
    }

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
