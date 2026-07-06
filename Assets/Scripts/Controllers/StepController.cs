using MCV_Module.Event;
using MCV_Module.GlobalManager;
using MCV_Module.StepSystem;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class StepController : ControllerBase<StepPanel>
    {
        protected override void OnViewBound()
        {
            EventBus<StepPreparedEvent>.Subscribe(OnStepPrepared);
            EventBus<StepWaitingEvent>.Subscribe(OnStepWaiting);
            EventBus<StepCompletedEvent>.Subscribe(OnStepCompleted);
            EventBus<StepTimeoutEvent>.Subscribe(OnStepTimeout);
        }

        private void OnStepPrepared(StepPreparedEvent evt)
        {
            var directors = GlobalStepSystemMgr.Instance.Directors;
            if (directors.Count > 0)
            {
                var step = directors[0].CurrentStep;
                if (step != null)
                {
                    View.SetStepInfo(step.displayName, step.description);
                    View.SetStatus("准备中...");
                }
            }
        }

        private void OnStepWaiting(StepWaitingEvent evt)
        {
            View.SetStatus("等待操作...");
        }

        private void OnStepCompleted(StepCompletedEvent evt)
        {
            View.SetStatus("✓ 完成");
        }

        private void OnStepTimeout(StepTimeoutEvent evt)
        {
            View.SetStatus($"⏱ 超时 ({evt.TimeoutSeconds}秒)");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus<StepPreparedEvent>.Unsubscribe(OnStepPrepared);
            EventBus<StepWaitingEvent>.Unsubscribe(OnStepWaiting);
            EventBus<StepCompletedEvent>.Unsubscribe(OnStepCompleted);
            EventBus<StepTimeoutEvent>.Unsubscribe(OnStepTimeout);
        }
    }
}
