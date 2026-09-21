using MCV_Module.Models.System;
using UnityEngine;

namespace MCV_Module.Models
{
    /// <summary>系统信息数据 SO（导出 StreamingAssets/Data/SystemData.json）。</summary>
    [CreateAssetMenu(menuName = "MCV/Data/SystemData", fileName = "SystemDataSO")]
    public class SystemDataSO : DataSO
    {
        public SystemData data = new SystemData();

        public override object CurrentData => data;

        [ContextMenu("导出到 JSON")] public override void Export() => ExportData();
    }
}
