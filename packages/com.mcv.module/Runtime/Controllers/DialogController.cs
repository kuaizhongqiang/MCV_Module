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
        /// <summary>
        /// 确认按钮点击：先播完收起动画，再发布结果事件。
        /// 避免先发布事件导致业务方（如场景状态切换）提前把面板失活，从而引发 StartCoroutine 报错。
        /// </summary>
        void HandleConfirm()
        {
            var title = GetTitle();
            View.Hide(() => EventBus<DialogResultEvent>.Publish(new DialogResultEvent(title, true)));
        }

        /// <summary>取消按钮点击：同样先收起动画，再发布结果。</summary>
        void HandleCancel()
        {
            var title = GetTitle();
            View.Hide(() => EventBus<DialogResultEvent>.Publish(new DialogResultEvent(title, false)));
        }
        #endregion

        string GetTitle()
        {
            return View.GetTitle();
        }
    }
}
