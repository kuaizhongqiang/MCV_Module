using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.UI.Panels;
using MCV_Module.Utils;

namespace MCV_Module.Controllers
{
    // WHY: 面板创建与 Show 必须由打开方（MenuController.OnResultClick）发起——面板懒加载创建，PanelBase.Start（进而 Bind）要等下一帧，打开方同帧拿到的 View 还是空的，所以「谁打开谁调 Show」；提交走事件驱动二次确认、结果按 DialogId 认领
    /// <summary>成绩预览控制器 —— 把 ResultSummitPanel 的按钮事件变成动作（关闭 / 提交）。</summary>
    public class ResultSummitController : ControllerBase<ResultSummitPanel>
    {
        // WHY: 结果按 DialogId 认领，Controller 常驻、订阅跨页残留，Id 须与其它发布方（DialogId.Exit / Back / BackToMenu / QuitApp）互不相同
        /// <summary>「提交成绩」确认框身份（DialogResultEvent 按它认领结果）。</summary>
        const DialogId SubmitDialogId = DialogId.SubmitScore;

        // WHY: 处理逻辑只发事件、不碰 View，故放 Awake 常驻订阅（不随面板重绑），先清后加防重复订阅
        /// <summary>常驻订阅对话框结果（Controller 常驻，订阅一次即可）。</summary>
        public override void OnInit()
        {
            base.OnInit();
            EventBus<DialogResultEvent>.Unsubscribe(OnDialogResult);
            EventBus<DialogResultEvent>.Subscribe(OnDialogResult);
        }

        public override void OnViewBound()
        {
            if (View == null) return;

            // 先退后订：面板每次重建都是新实例
            View.OnSubmitClicked -= OnSubmit;
            View.OnCloseClicked -= OnClose;

            View.OnSubmitClicked += OnSubmit;
            View.OnCloseClicked += OnClose;
        }

        public override void OnDispose()
        {
            if (View != null)
            {
                View.OnSubmitClicked -= OnSubmit;
                View.OnCloseClicked -= OnClose;
            }

            EventBus<DialogResultEvent>.Unsubscribe(OnDialogResult);
            base.OnDispose();
        }

        /// <summary>提交入口：先弹确认框（只发 DialogRequestEvent，由 DialogEventDispatcher 统一定位显示），不直接提交。</summary>
        void OnSubmit()
        {
            Log.Info("[ResultSummitController] 提交入口：请求提交确认");
            EventBus<DialogRequestEvent>.Publish(
                new DialogRequestEvent(SubmitDialogId, "确定要提交本次成绩吗？",
                    showConfirm: true, showCancel: true));
        }

        /// <summary>对话框结果：只认领本次提交确认（按 DialogId 区分），且只在「确认」时提交，取消 / 其它来源一律忽略。</summary>
        void OnDialogResult(DialogResultEvent result)
        {
            if (result == null || !result.Confirmed) return;
            if (result.Id != SubmitDialogId) return;

            SubmitScore();
        }

        // WHY: 本阶段只到按钮事件触发为止——成绩 JSON 已在打开预览时由 GlobalDataMgr.PreviewScore() 落盘，这里不做网络请求，上传实现接进来时改这里
        /// <summary>真正的提交动作（确认后才会走到这里）。</summary>
        void SubmitScore()
        {
            Log.Info("[ResultSummitController] 已确认，成绩提交入口被点击 —— 上传实现待接入（本阶段只到事件触发）");
        }

        /// <summary>关闭：只收起面板，不切场景、不销毁（下次打开复用同一实例）。</summary>
        void OnClose()
        {
            if (View == null) return;
            View.Hide();
        }
    }
}
