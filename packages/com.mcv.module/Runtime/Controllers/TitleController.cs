using MCV_Module.Controller;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controllers
{
    /// <summary>
    /// 标题面板控制器。
    /// TitlePanel 自行从 GlobalDataMgr 读取项目名并管理自身的显隐/动画，
    /// 当前无对外事件需要 Controller 调度，保留骨架以备后续扩展（如点击标题跳转等）。
    /// </summary>
    public class TitleController : ControllerBase<TitlePanel>
    {
        protected override void OnViewBound()
        {
            // TitlePanel 暂无需 Controller 订阅的事件
        }
    }
}
