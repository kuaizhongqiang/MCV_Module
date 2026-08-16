using MCV_Module.Models.Project;
using MCV_Module.Models.System;
using MCV_Module.Models.User;
using UnityEngine;

namespace MCV_Module.Models
{
    /// <summary>数据 SO 导出接口：供编辑器批量初始化扫描。</summary>
    public interface IDataExporter
    {
        void Export();
    }

    /// <summary>
    /// 数据 SO 基类（编辑器承载数据，运行走 JSON）。
    /// 注意：不用泛型基类存数据字段——Unity 序列化不支持泛型类型参数字段，
    /// 各具体 SO 自带数据字段，通过 ExportData 写入 StreamingAssets/Data/{类型名}.json。
    /// </summary>
    public abstract class DataSO : ScriptableObject, IDataExporter
    {
        /// <summary>把数据导出为 JSON（文件名取数据类型名，如 SystemData → SystemData.json）。
        /// 同步写入仅限 Editor（JsonReaderWriter.Write 为 Editor-only）。</summary>
        protected void ExportData<T>(T data) where T : class
        {
            if (data == null) return;
            string name = typeof(T).Name;
#if UNITY_EDITOR
            JsonReaderWriter.Write(name, data, null);
            Debug.Log($"[DataSO] 已导出 {name} → StreamingAssets/Data/{name}.json");
#endif
        }

        public abstract void Export();
    }

    [CreateAssetMenu(menuName = "MCV/Data/SystemData", fileName = "SystemDataSO")]
    public class SystemDataSO : DataSO
    {
        public SystemData data = new SystemData();
        [ContextMenu("导出到 JSON")] public override void Export() => ExportData(data);
    }

    [CreateAssetMenu(menuName = "MCV/Data/ProjectData", fileName = "ProjectDataSO")]
    public class ProjectDataSO : DataSO
    {
        public ProjectData data = new ProjectData();
        [ContextMenu("导出到 JSON")] public override void Export() => ExportData(data);
    }

    [CreateAssetMenu(menuName = "MCV/Data/UserData", fileName = "UserDataSO")]
    public class UserDataSO : DataSO
    {
        public UserData data = new UserData();
        [ContextMenu("导出到 JSON")] public override void Export() => ExportData(data);
    }

    [CreateAssetMenu(menuName = "MCV/Data/LanguageData", fileName = "LanguageDataSO")]
    public class LanguageDataSO : DataSO
    {
        public LanguageData data = new LanguageData();
        [ContextMenu("导出到 JSON")] public override void Export() => ExportData(data);
    }
}
