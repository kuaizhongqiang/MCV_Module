using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class RoamingBottomController : ControllerBase<RoamingBottomPanel>
    {
        protected override void OnViewBound()
        {
            View.SetForwardCallback(OnForward);
            View.SetBackwardCallback(OnBackward);
            View.SetExitRoamingCallback(OnExitRoaming);
        }

        private void OnForward()
        {
            // TODO: 漫游向前
        }

        private void OnBackward()
        {
            // TODO: 漫游向后
        }

        private void OnExitRoaming()
        {
            // TODO: 退出漫游模式
        }
    }
}
