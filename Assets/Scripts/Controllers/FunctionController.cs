using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class FunctionController : ControllerBase<FunctionPanel>
    {
        protected override void OnViewBound()
        {
            View.SetMenuCallback(OnMenu);
            View.SetResetCallback(OnReset);
            View.SetHelpCallback(OnHelp);
        }

        private void OnMenu()
        {
            // TODO: 返回主菜单
        }

        private void OnReset()
        {
            // TODO: 重置当前任务
        }

        private void OnHelp()
        {
            // TODO: 显示帮助信息
        }
    }
}
