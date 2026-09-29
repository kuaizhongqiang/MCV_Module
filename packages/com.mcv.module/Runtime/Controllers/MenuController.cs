using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Models.User;
using MCV_Module.UI.Panels;
using MCV_Module.UI.Tools;
using MCV_Module.Utils;

namespace MCV_Module.Controllers
{
    // WHY: 进入必须两步「先加载场景、就绪后再切状态」——切状态会清空所有 Canvas 面板，合成一步会先看到空房间再长出内容；退出走事件驱动二次确认，结果按 DialogId 认领，Id 必须全局唯一否则被别的常驻控制器同时认领
    /// <summary>菜单控制器：把菜单面板的入口按钮变成真实动作（进入漫游 / 进入器件内容页 / 退出 / 成绩预览）。</summary>
    public class MenuController : ControllerBase<MenuPanel>
    {
        // WHY: 场景名必须与 Resources/Config/SceneAAConfig 的 sceneName 一致，改名会加载不到房间
        /// <summary>漫游房间场景名。</summary>
        const string RoamingSceneName = "11_Room1";

        // WHY: 结果按 DialogId 认领，Controller 常驻、订阅跨页残留，Id 须与其它发布方（DialogId.Exit / Back / BackToMenu / SubmitScore）互不相同，否则被别的控制器同时认领
        /// <summary>「退出」确认框身份（DialogResultEvent 按它认领结果）。</summary>
        const DialogId QuitDialogId = DialogId.QuitApp;

        // WHY: 结果按 DialogId 认领，Id 须与其它发布方（DialogId.Exit / Back / BackToMenu / QuitApp / SubmitScore）互不相同
        /// <summary>「进入项目」确认框身份（房间项目 HUD 点击后弹出）。</summary>
        const DialogId EnterDialogId = DialogId.EnterProject;

        /// <summary>当前层级的兄弟列表（根层级时为根菜单）。</summary>
        readonly List<MenuClip> currentClips = new List<MenuClip>();

        /// <summary>当前层级的父菜单；null 表示当前处于根层级。</summary>
        MenuClip current;

        /// <summary>已请求加载、等待就绪的目标场景名；空串表示当前没有待进入的场景。</summary>
        string m_PendingSceneName = "";

        /// <summary>房间 HUD 已请求进入、等着用户在确认框上点「确认」的项目；null 表示没有待确认的进入请求。</summary>
        ProjectClip m_PendingEnterClip;

        public override void OnInit()
        {
            base.OnInit();
            // 场景加载完成 → 切界面（Controller 常驻，订阅一次即可，OnDestroy 退订）
            EventBus<SceneLoadedEvent>.Subscribe(OnSceneLoaded);
            // 退出确认的结果 → 真正退出（同上常驻订阅；先清后加防重复）
            EventBus<DialogResultEvent>.Unsubscribe(OnDialogResult);
            EventBus<DialogResultEvent>.Subscribe(OnDialogResult);
            // 房间项目 HUD 的「进入项目」请求 → 弹二次确认（同上常驻订阅：HUD 在房间场景里，随房间卸载而销毁）
            EventBus<RoomMenuEnterRequestEvent>.Unsubscribe(OnRoomMenuEnterRequested);
            EventBus<RoomMenuEnterRequestEvent>.Subscribe(OnRoomMenuEnterRequested);
        }

        public override void OnViewBound()
        {
            if (View == null) return;

            // 先退后订：面板每次重建都是新实例
            View.OnRoamingBtnClick -= OnRoamingClick;
            View.OnQuitBtnClick    -= OnQuitClick;
            View.OnResultBtnClick  -= OnResultClick;
            View.OnMenuBtnClick    -= OnMenuBtnClick;

            View.OnRoamingBtnClick += OnRoamingClick;
            View.OnQuitBtnClick    += OnQuitClick;
            View.OnResultBtnClick  += OnResultClick;
            View.OnMenuBtnClick    += OnMenuBtnClick;

            View.SetCopyright(GlobalUIMgr.IfCopyright, GlobalUIMgr.IfCompany);
        }

        public override void OnDispose()
        {
            if (View != null)
            {
                View.OnRoamingBtnClick -= OnRoamingClick;
                View.OnQuitBtnClick    -= OnQuitClick;
                View.OnResultBtnClick  -= OnResultClick;
                View.OnMenuBtnClick    -= OnMenuBtnClick;
            }
            EventBus<SceneLoadedEvent>.Unsubscribe(OnSceneLoaded);
            EventBus<DialogResultEvent>.Unsubscribe(OnDialogResult);
            EventBus<RoomMenuEnterRequestEvent>.Unsubscribe(OnRoomMenuEnterRequested);
            base.OnDispose();
        }

        #region 入口动作
        // WHY: 已在漫游时必须只收起弹层、不切状态——切状态会让 RoamingCanvas 整体重建，而重发场景请求会被 GlobalSceneMgr「已是当前切换场景」挡掉、OnSceneLoaded 不回调，导致 m_PendingSceneName 永久卡住
        /// <summary>漫游入口：请求加载房间场景；就绪后由 OnSceneLoaded 切到漫游界面，重复点击会被待进入状态挡掉。</summary>
        void OnRoamingClick()
        {
            // 已在漫游 → 「进入漫游」的等价结果就是回到漫游画面：收起菜单弹层即可
            if (GlobalDataMgr.GetProjectState() == ProjectState.Roaming)
            {
                if (View != null) View.SetUIActive(false);
                Log.Info("[MenuController] 已在漫游中，收起菜单弹层");
                return;
            }

            if (!string.IsNullOrEmpty(m_PendingSceneName))
            {
                Log.Warning($"[MenuController] 场景 {m_PendingSceneName} 正在加载中，忽略重复点击");
                return;
            }
            if (!GlobalSceneMgr.Exists || GlobalSceneMgr.Instance == null) return;

            m_PendingSceneName = RoamingSceneName;
            Log.Info($"[MenuController] 请求加载漫游场景：{RoamingSceneName}");
            EventBus<SceneSwitchRequestEvent>.Publish(new SceneSwitchRequestEvent(RoamingSceneName));
        }

        /// <summary>场景就绪：只有正好是本次待进入的场景才切界面，顺带清空待进入状态。</summary>
        void OnSceneLoaded(SceneLoadedEvent e)
        {
            if (string.IsNullOrEmpty(m_PendingSceneName)) return;
            if (e == null || e.SceneName != m_PendingSceneName) return;

            string sceneName = m_PendingSceneName;
            m_PendingSceneName = "";

            Log.Info($"[MenuController] 场景 {sceneName} 已就绪，切换到漫游界面");
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(SceneState.Roaming));
        }

        /// <summary>器件入口（菜单平铺化）：按 MenuClip.projectId 路由到 ProjectClip，再走共用的进入链路。</summary>
        void OnMenuBtnClick(MenuClip clip)
        {
            if (clip == null) return;

            ProjectClip project = GlobalDataMgr.GetProjectClip(clip.projectId);
            if (project == null)
            {
                Log.Warning($"[MenuController] 菜单 {clip.id} 的 projectId（{clip.projectId}）在 ProjectData.clips 中不存在，无法进入内容页");
                return;
            }

            EnterProject(project);
        }

        // WHY: 两步发布不可颠倒——TaskTypeChangeEventData 必须先写唯一源 currentTaskType，内容页重建时 TaskListController / ContentCanvas 才能取到正确的项目与任务
        /// <summary>进入某项目的内容页（菜单器件入口与房间项目 HUD 入口共用，只此一处实现）：写唯一源 currentClip，预设默认任务后切内容页 Canvas。</summary>
        void EnterProject(ProjectClip project)
        {
            if (project == null) return;

            GlobalDataMgr.SetCurrentClip(project);

            // 默认任务 = ProjectClip.Tasks 里第一个启用的任务（顺序即 TaskListPanel 的装配顺序）
            TaskType defaultType = ResolveDefaultTaskType(project);
            if (defaultType != TaskType.None)
            {
                // WHY: 走事件而非直写 ProjectData——currentTaskType 的唯一写入口在 GlobalUIMgr；当前是 Menu 态，它只写值、不会顺手重建菜单 Canvas
                EventBus<TaskTypeChangeEventData>.Publish(new TaskTypeChangeEventData(project, defaultType));
            }
            else
            {
                Log.Warning($"[MenuController] 器件 {project.id} 没有启用的任务，内容页将装配不出任务面板");
            }

            // 从漫游弹层 / 房间 HUD 进来：先卸掉房间场景（与 FunctionController 返回链路同一口径），避免再次进入时叠加实例
            if (GlobalDataMgr.GetProjectState() == ProjectState.Roaming &&
                GlobalSceneMgr.Exists && GlobalSceneMgr.Instance != null)
            {
                GlobalSceneMgr.Instance.UnloadSwitchedScene();
            }

            Log.Info($"[MenuController] 进入器件内容页：{project.id}（{project.displayName}）");
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(SceneState.UI));
        }

        /// <summary>默认任务类型：Tasks 里第一个 TaskActive 的任务；一个都没有时返回 None。</summary>
        static TaskType ResolveDefaultTaskType(ProjectClip project)
        {
            var tasks = project.Tasks;
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i] != null && tasks[i].TaskActive) return tasks[i].TaskType;
            }
            return TaskType.None;
        }

        // WHY: 预览面板走「激活 Canvas 懒加载」创建，不在此处销毁，关闭由面板自己 Hide()
        /// <summary>成绩预览入口：GlobalDataMgr.PreviewScore() 结算并落盘 → ScoreRecordFormatter.Build 装配数据 → 打开预览面板。</summary>
        void OnResultClick()
        {
            ResultSummitPanel panel = GlobalUIMgr.GetPanelOnActiveCanvas<ResultSummitPanel>();
            if (panel == null)
            {
                Log.Error("[MenuController] 无法取得 ResultSummitPanel（确认 Resources/UI/ResultSummitPanel.prefab 存在、且当前有激活 Canvas）");
                return;
            }

            ScoreData data = GlobalDataMgr.PreviewScore();
            if (data == null)
            {
                Log.Warning("[MenuController] 成绩结算失败，预览面板未打开");
                return;
            }

            panel.Show(ScoreRecordFormatter.Build(data, GlobalDataMgr.GetProjectName()));
            Log.Info($"[MenuController] 打开成绩预览：{data.FileName}，总分 {ScoreRecordFormatter.FormatScore(data.totalScore)}");
        }

        // WHY: 只发 DialogRequestEvent，由 DialogEventDispatcher 统一定位显示——菜单面板在 MenuCanvas 与漫游弹层两处复用，两处退出都走这条确认链路
        /// <summary>退出入口：先弹确认框，不直接退出。</summary>
        void OnQuitClick()
        {
            Log.Info("[MenuController] 退出入口：请求退出确认");
            EventBus<DialogRequestEvent>.Publish(
                new DialogRequestEvent(QuitDialogId, "确定要退出应用吗？",
                    showConfirm: true, showCancel: true));
        }

        /// <summary>对话框结果：只认领本控制器的退出确认（按 DialogId 区分），且只在「确认」时才退出，取消 / 其它来源一律忽略。</summary>
        void OnDialogResult(DialogResultEvent result)
        {
            if (result == null || !result.Confirmed) return;

            if (result.Id == QuitDialogId)
            {
                QuitApplication();
            }
            else if (result.Id == EnterDialogId)
            {
                // 取走即清：一次确认只进一个项目，避免下一次误用上次的目标
                ProjectClip target = m_PendingEnterClip;
                m_PendingEnterClip = null;
                EnterProject(target);
            }
        }

        // WHY: 退出统一走 AppQuitEvent，由 GlobalSceneMgr 先清理资源再退出——界面层不要直接 Application.Quit()（Editor 停播放的分支也在那个出口里）
        /// <summary>真正的退出：发布 AppQuitEvent。</summary>
        void QuitApplication()
        {
            Log.Info("[MenuController] 已确认，退出应用");
            EventBus<AppQuitEvent>.Publish(new AppQuitEvent());
        }

        // WHY: 必须由本常驻控制器统一处理——房间场景进内容页时被卸载、HUD 随之销毁，而确认结果要等用户点完才回来；Id 固定（不把项目名拼进去）才能与其它对话框的认领口径一致
        /// <summary>房间项目 HUD 的「进入项目」请求：先弹二次确认，确认后走与菜单器件按钮同一个 EnterProject。</summary>
        void OnRoomMenuEnterRequested(RoomMenuEnterRequestEvent e)
        {
            if (e == null || e.Clip == null) return;

            m_PendingEnterClip = e.Clip;

            Log.Info($"[MenuController] 房间 HUD 请求进入：{e.Clip.id}（{e.Clip.displayName}），待确认");
            EventBus<DialogRequestEvent>.Publish(
                new DialogRequestEvent(EnterDialogId, $"即将离开漫游，确定进入《{e.Clip.displayName}》吗？",
                    showConfirm: true, showCancel: true));
        }
        #endregion
    }
}
