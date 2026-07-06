using MCV_Module.Data.Project;
using MCV_Module.Event;
using MCV_Module.GlobalManager;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class TaskListController : ControllerBase<TaskPanel>
    {
        protected override void OnViewBound()
        {
            var clip = GlobalDataMgr.GetProjectClip();
            if (clip != null)
            {
                View.BuildTaskList(clip.Tasks, OnTaskSelected);
            }
        }

        private void OnTaskSelected(TaskType taskType)
        {
            EventBus<TaskChangedEvent>.Publish(new TaskChangedEvent(taskType));
        }
    }
}
