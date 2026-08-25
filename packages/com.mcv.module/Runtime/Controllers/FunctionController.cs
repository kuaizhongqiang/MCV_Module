using MCV_Module.Controller;
using MCV_Module.Utils;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controllers
{
    /// <summary>
    /// 功能面板控制器 —— 调度 FunctionPanel 的按钮事件。
    /// 仅在此绑定事件，具体业务逻辑（跳转/处理）暂不实现，留待后续填充。
    /// </summary>
    public class FunctionController : ControllerBase<FunctionPanel>
    {
        // 对话框标题（同时用于 DialogResultEvent 按 Title 区分是「退出」还是「返回」的确认）
        const string ExitDialogTitle = "退出";
        const string BackDialogTitle = "返回";

        protected override void OnViewBound()
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

        protected override void OnDestroy()
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
            base.OnDestroy();
        }

        // ───────────── 事件处理（具体业务逻辑待实现） ─────────────

        /// <summary>退出按钮：弹确认框，确认后退出应用。</summary>
        void OnExitClick()
        {
            EventBus<DialogRequestEvent>.Publish(
                new DialogRequestEvent(ExitDialogTitle, "确定要退出应用吗？当前进度将不会保存。",
                    showConfirm: true, showCancel: true));
        }

        /// <summary>
        /// 返回按钮：按 running-flow 的返回链路「Task → Menu → Login」逐级回退。
        /// 先根据当前 SceneState 判断「从哪返回哪」，再弹动态拼接的确认框。
        /// </summary>
        void OnBackClick()
        {
            // 当前所处界面
            SceneState current = GlobalUIMgr.GetCurrentState();

            // 解析返回目标（无上级可返回的状态直接忽略）
            SceneState targetState;
            string targetName;
            if (!ResolveBackTarget(current, out targetState, out targetName)) return;

            // 拼接「从<来源>返回<目标>」
            string source = DescribeCurrentSource(current);
            string message = $"确定从{source}返回{targetName}吗？";
            EventBus<DialogRequestEvent>.Publish(
                new DialogRequestEvent(BackDialogTitle, message,
                    showConfirm: true, showCancel: true));
        }

        /// <summary>
        /// 解析当前状态的返回目标（running-flow：Task → Menu → Login）。
        /// Task 态（UI/Roaming）返回到 Menu；Menu 态返回到 Login；其余无可返回目标。
        /// </summary>
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

        /// <summary>
        /// 拼接当前返回来源描述：Task 态用「项目名·任务类型」，Menu 态用「菜单界面」。
        /// 例：《电机维修实训》·仿真实验。数据未就绪时返回通用兜底文案。
        /// </summary>
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
                    projectName = clip?.displayName;

                    // 当前任务类型（转中文）
                    taskName = TaskTypeToChinese(GlobalDataMgr.Instance.ProjectData.currentTaskType);
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

        /// <summary>TaskType 枚举 → 中文名（与 EnumAll.cs 的 InspectorName 保持一致）。</summary>
        static string TaskTypeToChinese(TaskType type)
        {
            switch (type)
            {
                case TaskType.Purpose:        return "任务目的";
                case TaskType.Equipment:      return "实验仪器";
                case TaskType.Principle:      return "实验原理";
                case TaskType.LineConnection: return "电路连接";
                case TaskType.Training:       return "仿真实验";
                case TaskType.Test:           return "小测验";
                default:                      return "";
            }
        }

        /// <summary>SceneState 枚举 → 中文名。</summary>
        static string SceneStateToChinese(SceneState state)
        {
            switch (state)
            {
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

        /// <summary>
        /// 对话框结果处理：按 Title 区分是哪个确认框，Confirmed 为 true 时才执行真实动作。
        /// </summary>
        void OnDialogResult(DialogResultEvent result)
        {
            if (result == null || !result.Confirmed) return;

            if (result.Title == ExitDialogTitle)
            {
                ExitApplication();
            }
            else if (result.Title == BackDialogTitle)
            {
                GoBackByState();
            }
        }

        /// <summary>
        /// 真正的退出动作。
        /// Editor 下直接停止播放；真机下发布 AppQuitEvent，由 GlobalSceneMgr（最终出口）统一做资源清理后退出。
        /// </summary>
        void ExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 通过事件收口退出，不直接 Application.Quit()：由 GlobalSceneMgr.OnAppQuitRequested 处理资源清理后退出
            EventBus<AppQuitEvent>.Publish(new AppQuitEvent());
#endif
        }

        /// <summary>
        /// 真正的返回动作：按 running-flow 返回链路「Task → Menu → Login」逐级回退。
        /// 通过 SceneStateChangeEventData 切换状态（GlobalUIMgr 据此激活对应 Canvas）。
        /// </summary>
        void GoBackByState()
        {
            SceneState current = GlobalUIMgr.GetCurrentState();
            SceneState targetState;
            string targetName;
            if (!ResolveBackTarget(current, out targetState, out targetName)) return;

            Log.Info($"[FunctionController] 从{SceneStateToChinese(current)}返回{targetName}");
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(targetState));
        }
    }
}
