using MCV_Module.Event;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    /// <summary>
    /// 对话框控制器 —— 协调 DialogPanel 与业务系统。
    ///
    /// 显示触发：DialogRequestEvent 的显示统一由 DialogEventDispatcher（Event 层专门处理器）
    ///   经 GlobalUIMgr → 激活 Canvas → GetPanel&lt;DialogPanel&gt; 链路处理，本控制器不再订阅。
    ///
    /// 订阅：
    ///   DialogPanel 的 OnConfirm / OnCancel —— 用户操作
    ///
    /// 发布：
    ///   EventBus&lt;DialogResultEvent&gt; —— 对话框结果回传给请求方
    /// </summary>
    public class DialogController : ControllerBase<DialogPanel>
    {
        protected override void OnViewBound()
        {
            // 先清后加，避免重复订阅（框架可能重建 View）
            View.OnConfirm -= HandleConfirm;
            View.OnCancel -= HandleCancel;

            View.OnConfirm += HandleConfirm;
            View.OnCancel += HandleCancel;
        }

        protected override void OnDestroy()
        {
            if (View != null)
            {
                View.OnConfirm -= HandleConfirm;
                View.OnCancel -= HandleCancel;
            }
        }

        #region 事件处理
        /// <summary>确认按钮点击</summary>
        void HandleConfirm()
        {
            EventBus<DialogResultEvent>.Publish(new DialogResultEvent(GetTitle(), true));
            View.Hide();
        }

        /// <summary>取消按钮点击</summary>
        void HandleCancel()
        {
            EventBus<DialogResultEvent>.Publish(new DialogResultEvent(GetTitle(), false));
            View.Hide();
        }
        #endregion

        string GetTitle()
        {
            return View.GetTitle();
        }
    }
}
