using MCV_Module.Models.Project;
using UnityEngine;

namespace MCV_Module.Models
{
    /// <summary>项目数据 SO（导出 StreamingAssets/Data/ProjectData.json）。</summary>
    [CreateAssetMenu(menuName = "MCV/Data/ProjectData", fileName = "ProjectDataSO")]
    public class ProjectDataSO : DataSO
    {
        public ProjectData data = new ProjectData();

        public override object CurrentData => data;

        [ContextMenu("导出到 JSON")] public override void Export() => ExportData();
    }
}
