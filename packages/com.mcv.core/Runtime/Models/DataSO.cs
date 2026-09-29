using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Models
{
    // WHY: 不用泛型基类存数据字段——Unity 序列化不支持泛型类型参数字段，数据字段由各具体 SO 自带。
    /// <summary>数据 SO 基类（编辑器承载数据，运行走 JSON）。</summary>
    public abstract class DataSO : ScriptableObject, IDataExporter
    {
        // WHY: 同步写入仅限 Editor（JsonReaderWriter.Write 为 Editor-only）。
        /// <summary>把数据导出为 JSON（文件名取数据类型名，如 SystemData → SystemData.json）。</summary>
        protected void ExportData<T>(T data) where T : class
        {
            if (data == null) return;
            string name = typeof(T).Name;
#if UNITY_EDITOR
            JsonReaderWriter.Write(name, data, null);
            Log.Info($"[DataSO] 已导出 {name} → StreamingAssets/Data/{name}.json");
#endif
        }

        public abstract void Export();
    }
}
