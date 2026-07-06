using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class LeftBottomController : ControllerBase<LeftBottomPanel>
    {
        protected override void OnViewBound()
        {
            View.SetTaskToggleCallback(OnTaskToggle);
            View.SetRoamingToggleCallback(OnRoamingToggle);
        }

        private void OnTaskToggle()
        {
            // TODO: 切换任务列表显示/隐藏
        }

        private void OnRoamingToggle()
        {
            // TODO: 进入/退出漫游模式
        }
    }
}
