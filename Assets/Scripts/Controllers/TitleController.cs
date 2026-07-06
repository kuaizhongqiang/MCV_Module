using MCV_Module.Event;
using MCV_Module.GlobalManager;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class TitleController : ControllerBase<TitlePanel>
    {
        protected override void OnViewBound()
        {
            // 默认显示当前任务标题
            var taskData = GlobalDataMgr.GetTaskData(Data.Project.TaskType.Purpose);
            if (taskData != null)
            {
                View.UpdateTitle(taskData.TaskType);
                View.SetDescription(taskData.displayName);
            }
        }
    }
}
