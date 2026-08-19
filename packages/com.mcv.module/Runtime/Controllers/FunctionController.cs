using MCV_Module.Controller;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controllers
{
    /// <summary>
    /// 功能面板控制器 —— 调度 FunctionPanel 的按钮事件。
    /// 仅在此绑定事件，具体业务逻辑（跳转/处理）暂不实现，留待后续填充。
    /// </summary>
    public class FunctionController : ControllerBase<FunctionPanel>
    {
        protected override void OnViewBound()
        {
            // 先清后加，避免面板重建（Canvas 重挂）后重复订阅
            View.OnFunctionExitClick          -= OnExitClick;
            View.OnFunctionSettingClick        -= OnSettingClick;
            View.OnFunctionResourcePanelClick  -= OnResourcePanelClick;
            View.OnFunctionSummitClick         -= OnSummitClick;
            View.OnFunctionRecordClick         -= OnRecordClick;
            View.OnFunctionMuteClick           -= OnMuteClick;

            View.OnFunctionExitClick          += OnExitClick;
            View.OnFunctionSettingClick        += OnSettingClick;
            View.OnFunctionResourcePanelClick  += OnResourcePanelClick;
            View.OnFunctionSummitClick         += OnSummitClick;
            View.OnFunctionRecordClick         += OnRecordClick;
            View.OnFunctionMuteClick           += OnMuteClick;
        }

        protected override void OnDestroy()
        {
            if (View != null)
            {
                View.OnFunctionExitClick          -= OnExitClick;
                View.OnFunctionSettingClick        -= OnSettingClick;
                View.OnFunctionResourcePanelClick  -= OnResourcePanelClick;
                View.OnFunctionSummitClick         -= OnSummitClick;
                View.OnFunctionRecordClick         -= OnRecordClick;
                View.OnFunctionMuteClick           -= OnMuteClick;
            }
            base.OnDestroy();
        }

        // ───────────── 事件处理（具体业务逻辑待实现） ─────────────
        void OnExitClick()           { /* TODO: 退出逻辑 */ }
        void OnSettingClick()        { /* TODO: 设置逻辑 */ }
        void OnResourcePanelClick()  { /* TODO: 资源面板逻辑 */ }
        void OnSummitClick()         { /* TODO: 提交逻辑 */ }
        void OnRecordClick()         { /* TODO: 录制逻辑 */ }
        void OnMuteClick()           { /* TODO: 静音逻辑 */ }
    }
}
