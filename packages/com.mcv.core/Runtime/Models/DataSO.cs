using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Models
{
    /// <summary>
    /// 数据 SO 基类（编辑器承载数据，运行走 JSON）。
    /// 注意：不用泛型基类存数据字段——Unity 序列化不支持泛型类型参数字段，
    /// 各具体 SO 自带数据字段，通过 <see cref="ExportData"/> 写入导出后端（默认 JSON）。
    /// 具体 SO 见同目录 SystemDataSO / ProjectDataSO / UserDataSO / LanguageDataSO（各自独立文件）。
    /// </summary>
    public abstract class DataSO : ScriptableObject, IDataExporter
    {
        /// <summary>
        /// 导出后端（默认 JSON）。将来切 Lua 等格式时，**只替换这一个静态属性**，
        /// 上层 SO 与编辑器流水线都不用改（§7.2 L-4）。
        /// </summary>
        public static IDataExportBackend Backend { get; set; } = new JsonExportBackend();

        /// <summary>本 SO 承载的数据对象（导出与只读对账共用；具体 SO 返回各自的 data 字段）。</summary>
        public abstract object CurrentData { get; }

        /// <summary>把数据导出到后端介质（文件名取数据类型名，如 SystemData → SystemData.json）。</summary>
        public void ExportData()
        {
            object data = CurrentData;
            if (data == null)
            {
                Log.Warning($"[DataSO] {GetType().Name} 的数据为空，已跳过导出");
                return;
            }

            string name = data.GetType().Name;
            Backend.Write(name, data);
            Log.Info($"[DataSO] 已导出 {name} → {Backend.Describe(name)}");
        }

        /// <summary>
        /// 只读对账（**绝不写盘**）：介质内容与 SO 数据一致时返回 null，否则返回差异摘要。
        /// 供「导出前比对」「dry-run 菜单」使用 —— 数据没变就不写盘，避免无意义的时间戳/版本 diff。
        /// </summary>
        public string DryRun()
        {
            object data = CurrentData;
            if (data == null) return $"{GetType().Name}：数据为空";
            return Backend.Diff(data.GetType().Name, data);
        }

        public abstract void Export();
    }
}
