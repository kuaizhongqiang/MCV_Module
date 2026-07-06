using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class ControlInfoController : ControllerBase<ControlInfoPanel>
    {
        protected override void OnViewBound()
        {
            View.SetControlInfo(
                "WASD: 移动\n" +
                "鼠标: 视角\n" +
                "左键: 交互\n" +
                "ESC: 菜单"
            );
        }
    }
}
