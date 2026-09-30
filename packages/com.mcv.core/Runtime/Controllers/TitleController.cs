using MCV_Module.UI.Panels;

namespace MCV_Module.Controllers
{
    /// <summary>标题面板控制器（骨架）：TitlePanel 自行从 GlobalDataMgr 读项目名并管理显隐/动画，本控制器当前不调度任何事件。</summary>
    public class TitleController : ControllerBase<TitlePanel>
    {
        public override void OnViewBound()
        {
            // TitlePanel 暂无需 Controller 订阅的事件
        }
    }
}
