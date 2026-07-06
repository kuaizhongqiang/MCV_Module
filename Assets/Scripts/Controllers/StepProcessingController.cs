using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class StepProcessingController : ControllerBase<StepProcessingPanel>
    {
        protected override void OnViewBound()
        {
            // StepProcessingPanel 通过 EventBus 自驱动
        }
    }
}
