using MCV_Module.Data.Project;
using MCV_Module.Event;
using MCV_Module.GlobalManager;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class TaskEquipmentController : ControllerBase<TaskEquipmentPanel>
    {
        protected override void OnViewBound()
        {
            EventBus<TaskChangedEvent>.Subscribe(OnTaskChanged);
            LoadData();
        }

        private void LoadData()
        {
            var data = GlobalDataMgr.GetTaskData(TaskType.Equipment) as TaskEquipmentData;
            if (data != null) View.ShowEquipment(data);
        }

        private void OnTaskChanged(TaskChangedEvent evt)
        {
            if (evt.TaskType == TaskType.Equipment) LoadData();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus<TaskChangedEvent>.Unsubscribe(OnTaskChanged);
        }
    }
}
