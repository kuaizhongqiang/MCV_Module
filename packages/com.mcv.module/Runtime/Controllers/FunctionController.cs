using MCV_Module.Utils;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controllers
{
    /// <summary>功能面板控制器 —— 调度 FunctionPanel 的按钮事件（仅绑定事件；退出/返回走二次确认，设置/资源/提交/录制/静音为 TODO）。</summary>
    public class FunctionController : ControllerBase<FunctionPanel>
    {
        // 对话框身份（DialogResultEvent 按 DialogId 区分是「退出」还是「返回」的确认）
        const DialogId ExitDialogId = DialogId.Exit;
        const DialogId BackDialogId = DialogId.Back;

        // WHY: Controller 常驻, 订阅一次即可, 不再向 GlobalUIMgr 拉取当前状态
        /// <summary>当前导航状态（由 SceneStateChangeEventData 缓存）。</summary>
        SceneState m_CurrentState = SceneState.Setup;

        public override void OnInit()
        {
            base.OnInit();
            EventBus<SceneStateChangeEventData>.Unsubscribe(OnSceneStateChanged);
            EventBus<SceneStateChangeEventData>.Subscribe(OnSceneStateChanged);
        }

        /// <summary>导航状态变化：只更新本地缓存，供返回链路判断来源/目标。</summary>
        void OnSceneStateChanged(SceneStateChangeEventData e)
        {
            if (e == null) return;
            m_CurrentState = e.State;
        }

        public override void OnViewBound()
        {
            // 先清后加，避免面板重建（Canvas 重挂）后重复订阅
            View.OnFunctionExitClick           -= OnExitClick;
            View.OnFunctionBackClick           -= OnBackClick;
            View.OnFunctionSettingClick        -= OnSettingClick;
            View.OnFunctionResourcePanelClick  -= OnResourcePanelClick;
            View.OnFunctionSummitClick         -= OnSummitClick;
            View.OnFunctionRecordClick         -= OnRecordClick;
            View.OnFunctionMuteClick           -= OnMuteClick;

            View.OnFunctionExitClick           += OnExitClick;
            View.OnFunctionBackClick           += OnBackClick;
            View.OnFunctionSettingClick        += OnSettingClick;
            View.OnFunctionResourcePanelClick  += OnResourcePanelClick;
            View.OnFunctionSummitClick         += OnSummitClick;
            View.OnFunctionRecordClick         += OnRecordClick;
            View.OnFunctionMuteClick           += OnMuteClick;

            // 订阅对话框结果，处理「退出/返回」确认后的真实动作（常驻，先清后加防重复）
            EventBus<DialogResultEvent>.Unsubscribe(OnDialogResult);
            EventBus<DialogResultEvent>.Subscribe(OnDialogResult);
        }

        public override void OnDispose()
        {
            if (View != null)
            {
                View.OnFunctionExitClick           -= OnExitClick;
                View.OnFunctionBackClick           -= OnBackClick;
                View.OnFunctionSettingClick        -= OnSettingClick;
                View.OnFunctionResourcePanelClick  -= OnResourcePanelClick;
                View.OnFunctionSummitClick         -= OnSummitClick;
                View.OnFunctionRecordClick         -= OnRecordClick;
                View.OnFunctionMuteClick           -= OnMuteClick;
            }
            EventBus<DialogResultEvent>.Unsubscribe(OnDialogResult);
            EventBus<SceneStateChangeEventData>.Unsubscribe(OnSceneStateChanged);
            base.OnDispose();
        }

        // ───────────── 事件处理（具体业务逻辑待实现） ─────────────

        /// <summary>退出按钮：弹确认框，确认后退出应用。</summary>
        void OnExitClick()
        {
            EventBus<DialogRequestEvent>.Publish(
                new DialogRequestEvent(ExitDialogId, "确定要退出应用吗？当前进度将不会保存。",
                    showConfirm: true, showCancel: true));
        }

        // WHY: 按 running-flow「Task → Menu → Login」逐级回退, 先按当前 SceneState 判断「从哪返回哪」, 再弹动态拼接的确认框
        /// <summary>返回按钮。</summary>
        void OnBackClick()
        {
            // 当前所处界面（事件订阅缓存，不向 GlobalUIMgr 拉取）
            SceneState current = m_CurrentState;

            // 解析返回目标（无上级可返回的状态直接忽略）
            SceneState targetState;
            string targetName;
            if (!ResolveBackTarget(current, out targetState, out targetName)) return;

            // 拼接「从<来源>返回<目标>」
            string source = DescribeCurrentSource(current);
            string message = $"确定从{source}返回{targetName}吗？";
            EventBus<DialogRequestEvent>.Publish(
                new DialogRequestEvent(BackDialogId, message,
                    showConfirm: true, showCancel: true));
        }

        // WHY: running-flow 固定 Task → Menu → Login; Task 态(UI/Roaming)返回 Menu, Menu 态返回 Login, 其余无可返回目标
        /// <summary>解析当前状态的返回目标。</summary>
        bool ResolveBackTarget(SceneState current, out SceneState targetState, out string targetName)
        {
            targetState = SceneState.Setup;
            targetName = "";
            switch (current)
            {
                case SceneState.UI:
                case SceneState.Roaming:
                    targetState = SceneState.Menu;
                    targetName = "菜单界面";
                    return true;
                case SceneState.Menu:
                    targetState = SceneState.Login;
                    targetName = "登录界面";
                    return true;
                default:
                    // Start / Login / Setup 无可返回目标
                    return false;
            }
        }

        // WHY: 数据未就绪时返回通用兜底文案, 避免拼出「《》·空」这类残句
        /// <summary>拼接当前返回来源描述：Task 态用「项目名·任务类型」，Menu 态用「菜单界面」。</summary>
        string DescribeCurrentSource(SceneState current)
        {
            if (current == SceneState.UI || current == SceneState.Roaming)
            {
                string projectName = "";
                string taskName = "";

                // 数据源未就绪时安全降级，避免空引用
                if (GlobalDataMgr.Exists && GlobalDataMgr.Instance != null && GlobalDataMgr.Instance.ProjectData != null)
                {
                    // 当前项目名
                    var clip = GlobalDataMgr.GetProjectClip();
                    projectName = Localized.Name(clip);

                    // 当前任务类型（唯一源，转中文）
                    taskName = TaskTypeToChinese(GlobalDataMgr.GetCurrentTaskType());
                }

                if (!string.IsNullOrEmpty(projectName))
                {
                    return string.IsNullOrEmpty(taskName) ? $"《{projectName}》" : $"《{projectName}》·{taskName}";
                }

                if (!string.IsNullOrEmpty(taskName))
                {
                    return taskName;
                }

                return "当前任务";
            }

            if (current == SceneState.Menu)
            {
                return "菜单界面";
            }

            return SceneStateToChinese(current);
        }

        // WHY: 口径 = EnumAll.cs 的 InspectorName, **新增枚举值时必须同步这里**; None 返回空串, 调用方按"无任务名"处理
        /// <summary>TaskType 枚举 → 中文名。</summary>
        static string TaskTypeToChinese(TaskType type)
        {
            switch (type)
            {
                case TaskType.None:           return "";
                case TaskType.Purpose:        return "任务目的";
                case TaskType.Equipment:      return "实验仪器";
                case TaskType.Principle:      return "实验原理";
                case TaskType.LineConnection: return "电路连接";
                case TaskType.Training:       return "仿真实验";
                case TaskType.Test:           return "小测验";
                case TaskType.Info:           return "简介";
                case TaskType.Structure:      return "结构";
                case TaskType.Inspection:     return "检测";
                case TaskType.Exam:           return "考核";
                default:                      return "";
            }
        }

        // WHY: 口径 = EnumAll.cs 的 InspectorName, **新增枚举值时必须同步这里**; UI 取"任务界面"而非枚举字面量 "UI", 更贴合「从/返回 XX 界面」的文案
        /// <summary>SceneState 枚举 → 中文名。</summary>
        static string SceneStateToChinese(SceneState state)
        {
            switch (state)
            {
                case SceneState.Setup:   return "初始化界面";
                case SceneState.Start:   return "开始界面";
                case SceneState.Login:   return "登录界面";
                case SceneState.Menu:    return "菜单界面";
                case SceneState.UI:      return "任务界面";
                case SceneState.Roaming: return "漫游界面";
                default:                 return "当前界面";
            }
        }

        void OnSettingClick()        { /* TODO: 设置逻辑 */ }
        void OnResourcePanelClick()  { /* TODO: 资源面板逻辑 */ }
        void OnSummitClick()         { /* TODO: 提交逻辑 */ }
        void OnRecordClick()         { /* TODO: 录制逻辑 */ }
        void OnMuteClick()           { /* TODO: 静音逻辑 */ }

        // WHY: 按 DialogId 区分是哪个确认框, 必须 Confirmed 为 true 才执行真实动作
        /// <summary>对话框结果处理。</summary>
        void OnDialogResult(DialogResultEvent result)
        {
            if (result == null || !result.Confirmed) return;

            if (result.Id == ExitDialogId)
            {
                ExitApplication();
            }
            else if (result.Id == BackDialogId)
            {
                GoBackByState();
            }
        }

        // WHY: Editor 下直接停播, 真机下必须经 AppQuitEvent 由 GlobalSceneMgr(最终出口)做资源清理后退出
        /// <summary>真正的退出动作。</summary>
        void ExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 通过事件收口退出，不直接 Application.Quit()：由 GlobalSceneMgr.OnAppQuitRequested 处理资源清理后退出
            EventBus<AppQuitEvent>.Publish(new AppQuitEvent());
#endif
        }

        // WHY: 通过 SceneStateChangeEventData 切换状态(GlobalUIMgr 据此激活对应 Canvas), 不直接操作 Canvas
        /// <summary>真正的返回动作：按 running-flow「Task → Menu → Login」逐级回退。</summary>
        void GoBackByState()
        {
            SceneState current = m_CurrentState;
            SceneState targetState;
            string targetName;
            if (!ResolveBackTarget(current, out targetState, out targetName)) return;

            // WHY: 离开漫游必须先卸掉房间场景(AA 场景), 不卸载会导致再次进入时叠加出第二份房间实例
            if (current == SceneState.Roaming &&
                GlobalSceneMgr.Exists && GlobalSceneMgr.Instance != null)
            {
                GlobalSceneMgr.Instance.UnloadSwitchedScene();
            }

            Log.Info($"[FunctionController] 从{SceneStateToChinese(current)}返回{targetName}");
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(targetState));
        }
    }
}
