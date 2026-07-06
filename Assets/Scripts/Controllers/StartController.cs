using MCV_Module.Event;
using MCV_Module.GlobalManager;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class StartController : ControllerBase<StartPanel>
    {
        protected override void OnViewBound()
        {
            View.SetStartCallback(() =>
            {
                // 进入主菜单场景
                EventBus<TaskChangedEvent>.Publish(new TaskChangedEvent(Data.Project.TaskType.None));
            });
        }
    }
}
