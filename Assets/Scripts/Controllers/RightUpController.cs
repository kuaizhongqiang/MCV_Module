using MCV_Module.Event;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class RightUpController : ControllerBase<RightUpPanel>
    {
        protected override void OnViewBound()
        {
            EventBus<TaskChangedEvent>.Subscribe(OnTaskChanged);
            View.SetInfo("就绪");
        }

        private void OnTaskChanged(TaskChangedEvent evt)
        {
            View.SetInfo($"当前任务: {evt.TaskType}");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus<TaskChangedEvent>.Unsubscribe(OnTaskChanged);
        }
    }
}
