using MCV_Module.Models.System;
using UnityEngine;

namespace MCV_Module.Models
{
    /// <summary>多语言数据 SO（导出 StreamingAssets/Data/LanguageData.json）。</summary>
    [CreateAssetMenu(menuName = "MCV/Data/LanguageData", fileName = "LanguageDataSO")]
    public class LanguageDataSO : DataSO
    {
        public LanguageData data = new LanguageData();
        [ContextMenu("导出到 JSON")] public override void Export() => ExportData(data);
    }
}
