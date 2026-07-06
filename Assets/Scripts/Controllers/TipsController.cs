using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class TipsController : ControllerBase<TipsPanel>
    {
        protected override void OnViewBound()
        {
        }

        public void ShowTip(string message, float duration = -1)
        {
            View.ShowTip(message, duration);
        }
    }
}
