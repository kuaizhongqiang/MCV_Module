// 由 MCV Editor/创建/UI Panel 生成器生成（2026-09-16）—— 请按需补充业务代码
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.UI.Panels;
using MCV_Module.Utils;

namespace MCV_Module.Controllers
{
    // WHY: 返回确认走事件驱动 —— 点击只发 DialogRequestEvent, 定位链路统一收口在 DialogEventDispatcher(激活 Canvas → GetPanel&lt;DialogPanel&gt; → Show), 本类不自己找 DialogPanel / 不自己 SetActive
    // WHY: 内容页功能栏不再有 AI 入口 —— AI 面板由它自己的开关按钮(AiSwitchBtn → SetPanelActive)开合, 控制器不再管面板显隐
    /// <summary>内容页功能控制器 —— 把 ContentFunctionPanel 的「返回」(二次确认后回菜单) 变成动作。</summary>
    public class ContentFunctionController : ControllerBase<ContentFunctionPanel>
    {
        // WHY: 刻意不用 FunctionController 的 DialogId.Back —— Controller 常驻不随 Canvas 销毁, 切到内容页后 FunctionController 仍订着 DialogResultEvent, 同 Id 会被两边同时认领、发两次 SceneStateChangeEventData; Id 唯一是二者的隔离手段
        /// <summary>本页返回确认框的身份（DialogResultEvent 按它认领结果）。</summary>
        const DialogId BackDialogId = DialogId.BackToMenu;

        // WHY: Controller 常驻不销毁, 常驻订阅放 OnInit(只跑一次); 处理逻辑只发状态事件不碰 View, 无需随面板重绑, 先清后加防重复订阅
        /// <summary>常驻订阅对话框结果（DialogResultEvent）。</summary>
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
            View.OnBackBtnClick -= OnBackClick;
            View.OnBackBtnClick += OnBackClick;

            // 项目名在绑定后注入：面板不读数据，数据由 Controller 从唯一源取（项目为空时传空串，不显示脏数据）
            View.Init(Localized.Name(GlobalDataMgr.GetProjectClip()));

            View.SetCopyright(GlobalUIMgr.IfCopyright, GlobalUIMgr.IfCompany);
        }

        public override void OnDispose()
        {
            if (View != null)
            {
                View.OnBackBtnClick -= OnBackClick;
            }

            // 常驻订阅必须退订（Controller 常驻，仅随应用退出 / 场景卸载销毁）
            EventBus<DialogResultEvent>.Unsubscribe(OnDialogResult);
            base.OnDispose();
        }

        // WHY: 只发 DialogRequestEvent —— 由 DialogEventDispatcher 统一定位显示, 面板不存在时按其内建逻辑懒加载创建
        /// <summary>返回入口：先弹确认框，不直接换页。</summary>
        void OnBackClick()
        {
            Log.Info("[ContentFunctionController] 返回入口：请求返回确认");
            EventBus<DialogRequestEvent>.Publish(
                new DialogRequestEvent(BackDialogId, BuildBackMessage(),
                    showConfirm: true, showCancel: true));
        }

        // WHY: 数据源未就绪时安全降级为通用文案, 避免空引用
        /// <summary>确认文案：带上项目名让用户明确「要离开的是哪个器件」。</summary>
        static string BuildBackMessage()
        {
            string projectName = null;

            // 数据源未就绪时安全降级，避免空引用（与 FunctionController.DescribeCurrentSource 同口径）
            if (GlobalDataMgr.Exists && GlobalDataMgr.Instance != null && GlobalDataMgr.Instance.ProjectData != null)
            {
                projectName = GlobalDataMgr.GetProjectClip()?.displayName;
            }

            return string.IsNullOrEmpty(projectName)
                ? "确定要返回菜单界面吗？"
                : $"确定要离开《{projectName}》返回菜单界面吗？";
        }

        // WHY: 只认领本页返回确认(按 DialogId 区分), 取消 / 其它来源一律忽略; 必须 Confirmed 才换页
        /// <summary>对话框结果处理。</summary>
        void OnDialogResult(DialogResultEvent result)
        {
            if (result == null || !result.Confirmed) return;
            if (result.Id != BackDialogId) return;

            GoBackToMenu();
        }

        // WHY: 只发状态事件, 由 GlobalUIMgr 统一换 Canvas(不得自己 SetActive Canvas); 内容页不涉及漫游房间, 无需 UnloadSwitchedScene
        /// <summary>真正的返回动作：回菜单页。</summary>
        void GoBackToMenu()
        {
            Log.Info("[ContentFunctionController] 已确认，返回菜单页");
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(SceneState.Menu));
        }
    }
}
