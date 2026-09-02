// 由 MCV/创建/UI Panel 生成器生成（2026-09-01）—— 请按需补充业务代码
using MCV_Module.UI;
using MCV_Module.UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>ResourcePanel 面板</summary>
    [RequireController(typeof(MCV_Module.Controllers.ResourceController))]
    public class ResourcePanel : PanelBase
    {
        [SerializeField] Transform contentParent;
        [SerializeField] VideoResouceContent currentVideoContent;
        [SerializeField] ImageResouceContent currentImageContent;
        [SerializeField] Button closeBtn;
        [SerializeField] Transform ListParent;                        // 这里获取和排列的方法和RecordPanel一样
        static readonly string[] RecordListLabelPaths = { "UI/RecordListLabelOne", "UI/RecordListLabelTwo", "UI/RecordListLabelThree" };
    }
}
