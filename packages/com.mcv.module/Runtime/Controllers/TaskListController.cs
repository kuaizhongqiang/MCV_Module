using MCV_Module.Controller;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controllers
{
    public class TaskListController : ControllerBase<TaskListPanel>
    {
        protected override void Awake()
        {
            base.Awake();
            // EventBus 订阅去重（Contains 判断），Controller 常驻，一次订阅即可
            EventBus<TaskTypeChangeEventData>.Subscribe(OnTaskChanged);
        }

        protected override void OnDestroy()
        {
            EventBus<TaskTypeChangeEventData>.Unsubscribe(OnTaskChanged);
            base.OnDestroy();
        }

        /// <summary>每次面板重建绑定后：按当前项目与任务类型装配任务列表。</summary>
        protected override void OnViewBound()
        {
            var project = GlobalDataMgr.GetProjectClip();
            if (project == null) return;
            View.Init(project, GlobalDataMgr.Instance.ProjectData.currentTaskType);
        }

        /// <summary>
        /// 任务切换：先同步当前任务状态（逻辑），再刷新列表显示（显示）。
        /// GlobalUIMgr 已监听同一事件负责 UI 重建；此处兜底同步面板显示（面板可能已重建，用 Unity 假空判断）。
        /// </summary>
        void OnTaskChanged(TaskTypeChangeEventData e)
        {
            if (e == null) return;
            GlobalDataMgr.Instance.ProjectData.currentTaskType = e.TaskType;
            if (View != null) View.SetTaskType(e.TaskType);
        }
    }
}
