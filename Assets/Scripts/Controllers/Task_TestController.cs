using MCV_Module.Data.Project;
using MCV_Module.Event;
using MCV_Module.GlobalManager;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class TaskTestController : ControllerBase<TaskTestPanel>
    {
        protected override void OnViewBound()
        {
            EventBus<TaskChangedEvent>.Subscribe(OnTaskChanged);
            LoadData();
        }

        private void LoadData()
        {
            var data = GlobalDataMgr.GetTaskData(TaskType.Test) as TaskTestData;
            if (data != null) View.ShowQuestion(data);
        }

        private void OnTaskChanged(TaskChangedEvent evt)
        {
            if (evt.TaskType == TaskType.Test) LoadData();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus<TaskChangedEvent>.Unsubscribe(OnTaskChanged);
        }
    }
}
