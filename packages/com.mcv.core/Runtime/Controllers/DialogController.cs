using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controllers
{
    // WHY: DialogRequestEvent 的显示统一由 DialogEventDispatcher(Event 层专门处理器, 经 GlobalUIMgr → 激活 Canvas → GetPanel&lt;DialogPanel&gt;)处理, 本控制器不再订阅显示触发
    /// <summary>对话框控制器 —— 协调 DialogPanel 与业务系统：订阅 OnConfirm / OnCancel，并以 DialogResultEvent 回传结果。</summary>
    public class DialogController : ControllerBase<DialogPanel>
    {
        public override void OnViewBound()
        {
            // 先清后加，避免重复订阅（框架可能重建 View）
            View.OnConfirm -= HandleConfirm;
            View.OnCancel -= HandleCancel;

            View.OnConfirm += HandleConfirm;
            View.OnCancel += HandleCancel;
        }

        public override void OnDispose()
        {
            if (View != null)
            {
                View.OnConfirm -= HandleConfirm;
                View.OnCancel -= HandleCancel;
            }
        }

        #region 事件处理
        // WHY: 必须先播完收起动画再发结果事件 —— 先发会让业务方(如场景状态切换)提前把面板失活, 面板自身的收起协程随即报错
        /// <summary>确认按钮点击。</summary>
        void HandleConfirm()
        {
            DialogId id = GetDialogId();
            View.Hide(() => EventBus<DialogResultEvent>.Publish(new DialogResultEvent(id, true)));
        }

        /// <summary>取消按钮点击：同样先收起动画，再发布结果。</summary>
        void HandleCancel()
        {
            DialogId id = GetDialogId();
            View.Hide(() => EventBus<DialogResultEvent>.Publish(new DialogResultEvent(id, false)));
        }

        // WHY: 必须在 Hide 之前取 —— 与旧「收起前读标题」同口径，身份随本次对话框行走，不依赖面板存活
        /// <summary>当前对话框身份：View 为空（面板已销毁）时回退 None。</summary>
        DialogId GetDialogId()
        {
            return View != null ? View.CurrentId : DialogId.None;
        }
        #endregion
    }
}
