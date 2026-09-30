using MCV_Module.Models;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.TaskObj
{
    // WHY: 高亮表现已从框架移除——宿主若要悬停/接触表现，自行订阅本类的 MoEnter / MoExit 实现
    /// <summary>待检测点（表笔要碰的目标）：管"我是哪个元件的哪个点"，点名供接触事件的订阅方读取</summary>
    public class InspectionElementPointObj : InteractiveBase
    {
        [SerializeField] ElementType element = ElementType.None;
        [SerializeField] ElementPointNameType point = ElementPointNameType.None;
        string pointName;

        protected override void Awake()
        {
            base.Awake();
            // WHY: 点名只在 Awake 取一次——之后改 gameObject.name 不会同步，名字要在进检测任务前定好
            pointName = $"{ChnNameMap.Get(element)}_{gameObject.name}";
        }

        /// <summary>点名（显示用）：元件中文名_检测点物体名；表笔接触事件的订阅方读它</summary>
        public string GetPointName()
        {
            return pointName;
        }
    }
}
