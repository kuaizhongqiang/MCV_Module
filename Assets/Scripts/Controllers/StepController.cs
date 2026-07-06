using MCV_Module.Event;
using MCV_Module.GlobalManager;
using MCV_Module.StepSystem;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class StepController : ControllerBase<StepPanel>
    {
        private StepDirector _currentDirector;

        protected override void OnViewBound()
        {
            // 获取当前场景的 StepDirector
            EventBus<StepPreparedEvent>.Subscribe(OnStepPrepared);
        }

        private void OnStepPrepared(StepPreparedEvent evt)
        {
            var directors = GlobalStepSystemMgr.Instance.Directors;
            if (directors.Count > 0)
            {
                _currentDirector = directors[0];
                var step = _currentDirector.CurrentStep;
                if (step != null)
                {
                    View.SetStepInfo(step.displayName, step.description);
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus<StepPreparedEvent>.Unsubscribe(OnStepPrepared);
        }
    }
}
