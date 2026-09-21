using MCV_Module.Models.User;
using UnityEngine;

namespace MCV_Module.Models
{
    /// <summary>用户数据 SO（导出 StreamingAssets/Data/UserData.json）。</summary>
    [CreateAssetMenu(menuName = "MCV/Data/UserData", fileName = "UserDataSO")]
    public class UserDataSO : DataSO
    {
        public UserData data = new UserData();

        public override object CurrentData => data;

        [ContextMenu("导出到 JSON")] public override void Export() => ExportData();
    }
}
