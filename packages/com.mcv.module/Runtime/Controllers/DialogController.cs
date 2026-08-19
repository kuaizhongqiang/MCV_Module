using MCV_Module.Event;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    /// <summary>
    /// 对话框控制器 —— 协调 DialogPanel 与业务系统。
    ///
    /// 订阅：
    ///   EventBus&lt;DialogRequestEvent&gt; —— 任意系统请求打开对话框
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

            EventBus<DialogRequestEvent>.Subscribe(OnDialogRequested);
        }

        protected override void OnDestroy()
        {
            if (View != null)
            {
                View.OnConfirm -= HandleConfirm;
                View.OnCancel -= HandleCancel;
            }
            EventBus<DialogRequestEvent>.Unsubscribe(OnDialogRequested);
        }

        #region 事件处理
        /// <summary>外部系统请求打开对话框</summary>
        void OnDialogRequested(DialogRequestEvent request)
        {
            if (View == null) return;
            View.Show(request);
        }

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
