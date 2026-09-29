using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controllers
{
    public class TaskListController : ControllerBase<TaskListPanel>
    {
        public override void OnInit()
        {
            base.OnInit();
            // EventBus 订阅去重（Contains 判断），Controller 常驻，一次订阅即可
            EventBus<TaskTypeChangeEventData>.Subscribe(OnTaskChanged);
        }

        public override void OnDispose()
        {
            EventBus<TaskTypeChangeEventData>.Unsubscribe(OnTaskChanged);
            base.OnDispose();
        }

        /// <summary>每次面板重建绑定后：按当前项目与任务类型装配任务列表（任务类型读唯一源）。</summary>
        public override void OnViewBound()
        {
            var project = GlobalDataMgr.GetProjectClip();
            if (project == null) return;
            View.Init(project, GlobalDataMgr.GetCurrentTaskType());
        }

        // WHY: "当前任务类型"的唯一写入点是 GlobalUIMgr（处理 TaskTypeChangeEventData 时写 ProjectData），这里不再重复写，避免两处状态源
        /// <summary>任务切换：刷新列表显示（只读，不写状态）。</summary>
        void OnTaskChanged(TaskTypeChangeEventData e)
        {
            if (e == null) return;
            if (View != null) View.SetTaskType(e.TaskType);
        }
    }
}
