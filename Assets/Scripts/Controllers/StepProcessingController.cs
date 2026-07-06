using MCV_Module.Event;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class StepProcessingController : ControllerBase<StepProcessingPanel>
    {
        protected override void OnViewBound()
        {
            EventBus<ProcessChangedEvent>.Subscribe(OnProcessChanged);
            EventBus<StepCompletedEvent>.Subscribe(OnStepCompleted);
        }

        private void OnProcessChanged(ProcessChangedEvent evt)
        {
            View.SetProcessingName($"工序: {evt.ProcessingId}");
        }

        private void OnStepCompleted(StepCompletedEvent evt)
        {
            View.SetProgressText($"步骤 {evt.StepId} 完成");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus<ProcessChangedEvent>.Unsubscribe(OnProcessChanged);
            EventBus<StepCompletedEvent>.Unsubscribe(OnStepCompleted);
        }
    }
}
