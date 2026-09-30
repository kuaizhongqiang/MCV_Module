using System.Collections;
using MCV_Module.Utils;
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Singleton;
using MCV_Module.UI;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Managers
{
    /// <summary>UI 管理器：Canvas 注册表 + 状态事件驱动重建（控制器与 Canvas 经此解耦）。</summary>
    public class GlobalUIMgr : SingletonGlobalMgr<GlobalUIMgr>
    {
        #region 参数
        [SerializeField] Models.PlayMode playMode = Models.PlayMode.Debug;
        Dictionary<string, CanvasBase> canvasDict = new Dictionary<string, CanvasBase>();
        /// <summary>当前导航状态（唯一源）：只由 SceneStateChangeEventData 驱动更新，对外只广播不带查询。</summary>
        SceneState m_CurrentState = SceneState.Setup;
        CanvasBase m_ActiveCanvas;
        /// <summary>进行中的状态/任务切换协程（用于连续切换时取消上一次，避免动画叠加）。</summary>
        Coroutine m_SwitchCoroutine;

        [Header("初始状态"), Tooltip("UI 就绪后自动发布的初始 SceneState（状态系统落地前引导）")]
        [SerializeField] SceneState m_InitialState = SceneState.Start;
        // WHY: 当前任务类型的唯一源是 ProjectData.currentTaskType，本字段只为 Inspector 兼容，不再作运行态来源
        [SerializeField] TaskType m_InitialTaskType = TaskType.None;
        bool m_InitialStatePublished = false;

        [SerializeField,Header("版权信息"),Tooltip("这个部分要在启动的时候通过加密内容来实现")] bool ifCopyright = true;
        [SerializeField] bool ifCompany = true;

        public static bool IfCopyright {get => Instance.ifCopyright; set => Instance.ifCopyright = value;}
        public static bool IfCompany {get => Instance.ifCompany; set => Instance.ifCompany = value;}
        #endregion

        #region 生命周期
        protected override IEnumerator DelayInit()
        {
            // 按运行模式开关屏幕调试浮层（经 Log 统一控制，可安全地在浮层尚未创建时调用）
            if (playMode == Models.PlayMode.Debug)
            {
                Log.EnableGui();
            }
            else
            {
                Log.DisableGui();
            }

            // 状态事件驱动 Canvas 初始化（强引用，OnDestroy 必须退订）
            EventBus<SceneStateChangeEventData>.Subscribe(OnSceneStateChanged);
            EventBus<TaskTypeChangeEventData>.Subscribe(OnTaskTypeChanged);
            // 登录成功 → 进入 Menu（登录→菜单导航断点的监听方，常驻订阅）
            EventBus<LoginSuccessEvent>.Subscribe(OnLoginSuccess);
            yield return null;
            // GlobalUIMgr 就绪后，启动对话框专门处理逻辑（依赖激活 Canvas 与 GetPanel 链路）
            DialogEventDispatcher.Initialize();
            isInit = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DialogEventDispatcher.Shutdown();
            EventBus<SceneStateChangeEventData>.Unsubscribe(OnSceneStateChanged);
            EventBus<TaskTypeChangeEventData>.Unsubscribe(OnTaskTypeChanged);
            EventBus<LoginSuccessEvent>.Unsubscribe(OnLoginSuccess);
        }
        #endregion

        #region 静态方法
        public static void RegisterCanvas(CanvasBase canvas)
        {
            string name = canvas.GetType().ToString();
            if (!Instance.canvasDict.ContainsKey(name))
            {
                Instance.canvasDict.Add(name, canvas);
            }
            Instance.TryPublishInitialState(); // 首个 Canvas 注册后延迟发布初始状态
        }

        public static void UnregisterCanvas(CanvasBase canvas)
        {
            // 单例已销毁（退出 Play / 场景切换）时直接返回，避免 Canvas.OnDestroy 空引用
            if (!Exists || Instance == null) return;
            string name = canvas.GetType().ToString();
            if (Instance.canvasDict.ContainsKey(name))
            {
                Instance.canvasDict.Remove(name);
            }
        }

        public static T GetCanvas<T>() where T : CanvasBase
        {
            string name = typeof(T).ToString();
            if (Instance.canvasDict.ContainsKey(name))
            {
                return Instance.canvasDict[name] as T;
            }
            return null;
        }

        /// <summary>获取当前激活（正在展示）的 Canvas。状态切换时由 OnSceneStateChanged 维护。</summary>
        public static CanvasBase GetActiveCanvas()
        {
            if (!Exists || Instance == null) return null;
            return Instance.m_ActiveCanvas;
        }

        /// <summary>在当前激活的 Canvas 上获取（必要时懒加载创建）指定面板。</summary>
        public static T GetPanelOnActiveCanvas<T>() where T : PanelBase
        {
            var canvas = GetActiveCanvas();
            if (canvas == null) return null;
            return canvas.GetPanel<T>();
        }

        public static T GetPanel<T>() where T : PanelBase
        {
            foreach (var canvas in Instance.canvasDict.Values)
            {
                // WHY: 跳过未激活的 Canvas（避免在隐藏画布下建面板）与常驻画布 LoadingCanvas（其面板由 LoadingController 驱动）
                if (!canvas.isActiveAndEnabled || canvas.IsPersistent) continue;
                T panel = canvas.GetPanel<T>();
                if (panel != null)
                {
                    return panel;
                }
            }
            return null;
        }
        #endregion

        #region 私有方法
        /// <summary>SceneState 变化（事件驱动唯一入口）：带淡入淡出过渡切换到目标 Canvas。</summary>
        void OnSceneStateChanged(SceneStateChangeEventData e)
        {
            SwitchToState(e.State);
        }

        /// <summary>登录成功：进入 Menu 状态（LoginSuccessEvent 唯一监听方，常驻订阅，不会随 Canvas 销毁）。</summary>
        void OnLoginSuccess(LoginSuccessEvent e)
        {
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(SceneState.Menu));
            Log.Info("[GlobalUIMgr] 登录成功，进入菜单界面");
        }

        /// <summary>TaskType 变化：写入唯一源；仅在任务态（UI / Roaming）重建激活 Canvas 装配新面板。</summary>
        void OnTaskTypeChanged(TaskTypeChangeEventData e)
        {
            GlobalDataMgr.SetCurrentTaskType(e.TaskType);

            if (m_ActiveCanvas == null) return;
            if (m_CurrentState != SceneState.UI && m_CurrentState != SceneState.Roaming) return;

            SwitchToState(m_CurrentState);
        }

        /// <summary>状态切换入口：淡出当前 → 重建目标 → 淡入目标；同一时间只保留一个切换协程。</summary>
        void SwitchToState(SceneState state)
        {
            // WHY: 常驻画布（LoadingCanvas）不进这张表——它的加载遮挡层必须活过整个状态切换
            var all = new List<CanvasBase>();
            foreach (var canvas in canvasDict.Values)
            {
                if (canvas == null || canvas.IsPersistent) continue;
                all.Add(canvas);
            }

            var target = all.Find(c => c.MatchesState(state));
            if (target == null)
            {
                // WHY: 静默 return 会让"初始状态早于目标 Canvas 注册"这类时序问题完全无迹可循（曾导致所有 Canvas 全亮、首屏不启动）
                Log.Warning($"[GlobalUIMgr] 状态 {state} 没有匹配的 Canvas（注册 {canvasDict.Count} 个、参与切换 {all.Count} 个），本次不切换");
                return;
            }

            // WHY: 项目状态须在 Canvas 重建之前写入——面板在 Awake 里就要据此决定显示哪些入口（见 MenuPanel.SetBtnsActive）
            GlobalDataMgr.SetProjectState(ToProjectState(state));

            if (m_SwitchCoroutine != null)
            {
                StopCoroutine(m_SwitchCoroutine);
                m_SwitchCoroutine = null;
            }
            m_SwitchCoroutine = StartCoroutine(SwitchToStateCoroutine(target, state, all));
        }

        /// <summary>SceneState → ProjectState 映射：ProjectState 是面板侧读取的「当前所处页面」口径。</summary>
        static ProjectState ToProjectState(SceneState state)
        {
            switch (state)
            {
                case SceneState.Menu:    return ProjectState.Menu;
                case SceneState.UI:      return ProjectState.UI;
                case SceneState.Roaming: return ProjectState.Roaming;
                default:                 return ProjectState.Start; // Setup / Start / Login
            }
        }

        IEnumerator SwitchToStateCoroutine(CanvasBase target, SceneState state, List<CanvasBase> all)
        {
            var prev = m_ActiveCanvas;
            m_CurrentState = state;

            // 1) 淡出当前激活的 Canvas（无论是否同一目标，都先淡出以保证过渡效果）
            if (prev != null && prev.isActiveAndEnabled)
            {
                prev.SetUIActive(false);
                yield return new WaitForSeconds(prev.AnimDuration);
            }

            // 2) 隐藏所有非目标 Canvas（含刚淡出的 prev），并清空各自面板
            foreach (var canvas in all)
            {
                if (canvas != target)
                {
                    canvas.SetUIActiveImmediately(false);
                }
                canvas.ClearPanels();
            }

            // 3) 激活目标 Canvas → 重建面板 → 淡入
            m_ActiveCanvas = target;
            if (!target.gameObject.activeSelf)
            {
                target.gameObject.SetActive(true);
            }
            target.Rebuild();
            CanvasRebuildVersion++;     // 面板已重建完：等到这个版本变化的消费方现在可以安全地建自己的面板了
            target.SetUIActive(true);

            m_SwitchCoroutine = null;
        }

        // WHY: 检测预制体在任务切换事件里立刻实例化（比画布重建早约 0.3s），步骤链当场开跑会让 Start 弹的 StepUIPanel 被下一次 ClearPanels() 销毁；在 Manager 层暴露版本号，避免 Manager 反向依赖 UI 层的 CanvasBase
        /// <summary>画布重建版本号：每次「状态切换 → 目标画布 Rebuild 完成」+1，供装配早于重建的消费方等待。</summary>
        public static int CanvasRebuildVersion { get; private set; }

        /// <summary>是否正处于状态切换中（切换期建的面板会被 ClearPanels() 清掉；不在切换即可直接建面板）。</summary>
        public static bool IsSwitching => Exists && Instance.m_SwitchCoroutine != null;

        // WHY: 触发条件是"初始状态的目标 Canvas 已注册"，**不是"首个 Canvas 注册"** ——
        //      LoadingCanvas 是常驻画布、由 LoadingController 在 UI 包预加载期间运行时补建，它会先于 1_Content 注册；
        //      若按"首个 Canvas"发布，此刻注册表里只有常驻画布，而 SwitchToState 会跳过常驻画布 → 匹配目标为 null → 静默 return，
        //      且标记已置位、此后永不重发，结果就是"所有 Canvas 全亮、Start 页不启动"。故每次都重试到目标画布到位为止。
        /// <summary>UI 就绪后发布初始状态，触发首次重建（目标 Canvas 尚未注册时留待其注册回调再次尝试）。</summary>
        void TryPublishInitialState()
        {
            if (m_InitialStatePublished) return;

            // 目标 Canvas 还没注册：不置位、不发布，等它注册时再进来（见 RegisterCanvas 的调用点）
            if (!HasCanvasForState(m_InitialState)) return;

            m_InitialStatePublished = true;
            StartCoroutine(PublishInitialState());
        }

        /// <summary>注册表里是否已有匹配指定状态的画布（只算参与状态切换的非常驻画布）。</summary>
        bool HasCanvasForState(SceneState state)
        {
            foreach (var canvas in canvasDict.Values)
            {
                if (canvas == null || canvas.IsPersistent) continue;
                if (canvas.MatchesState(state)) return true;
            }
            return false;
        }

        // WHY: 兜底 —— 初始状态的目标 Canvas 始终没出现（配置错误 / 场景没加载）时，等满 15 秒也要发布一次，
        //      避免整个 UI 永远停在"所有 Canvas 全亮"的未初始化态；正常情况下这条分支不会走到。
        IEnumerator PublishInitialState()
        {
            float guard = 0f;
            while (!HasCanvasForState(m_InitialState) && guard < 15f)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!HasCanvasForState(m_InitialState))
                Log.Warning($"[GlobalUIMgr] 等待初始状态 {m_InitialState} 的目标 Canvas 超时（15s），仍发布一次（可能找不到目标画布）");

            Log.Info($"[GlobalUIMgr] 发布初始状态：{m_InitialState}（订阅者 {EventBus<SceneStateChangeEventData>.SubscriberCount} 个，注册画布 {canvasDict.Count} 个）");
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(m_InitialState));

            // WHY: 若发布时确实无人接收（订阅尚未挂上），复位标记等待后续 Canvas 注册时补发，避免初始状态永久丢失
            if (EventBus<SceneStateChangeEventData>.SubscriberCount == 0)
            {
                Log.Warning("[GlobalUIMgr] 初始状态发布时无订阅者，已复位标记等待补发");
                m_InitialStatePublished = false;
            }
        }
        #endregion

        #region 界面内容注入AI 提示词
        public static string CurrentStateDescription()
        {
            if (!Exists || Instance == null) return "";

            var mgr = Instance;
            string result = "";
            result += "当前处于：" + SceneStateDescription(mgr.m_CurrentState) + "\n";

            // 仅在进入功能场景(UI/漫游)时才附加任务上下文
            if (mgr.m_CurrentState == SceneState.UI || mgr.m_CurrentState == SceneState.Roaming)
            {
                result += "当前任务：" + GlobalDataMgr.GetCurrentTaskType().ToString() + "\n";
                string panelDesc = TaskPanelDescription();
                if (!string.IsNullOrEmpty(panelDesc))
                {
                    result += "当前任务面板：" + panelDesc + "\n";
                }
            }
            return result;
        }

        static string SceneStateDescription(SceneState state)
        {
            switch (state)
            {
                case SceneState.Setup:
                    return "初始化界面，无法操作";
                case SceneState.Start:
                    return "欢迎界面，可以点击进入按钮进入";
                case SceneState.Login:
                    return "登录界面，三种登录形式，游客/学生/教师，其中游客不需要账户密码即可登录";
                case SceneState.Menu:
                    return "菜单界面，可以根据所需进入对应模块进行学习";
                case SceneState.UI:
                    return "UI界面，主要进行平面交互";
                case SceneState.Roaming:
                    return "漫游界面，主要进行三维交互";
                default:
                    return "未知";
            }
        }

        // WHY: 一律用 FindPanel（只查不建）而不是 GetPanel —— GetPanel 查不到时会**实例化**一个面板挂到画布上
        //      （CanvasBase.CreatePanel），而本方法只是"把当前界面描述给 AI 看"，不该有任何副作用。
        //      实测症状：TaskType.Info / Structure / Inspection 落在 default 分支 → 每次组装 AI 上下文都凭空冒出一个
        //      TaskDefaultPanel；漫游页（激活画布是 RoamingCanvas）上同样会往漫游画布挂面板。
        static string TaskPanelDescription()
        {
            var canvas = Instance.m_ActiveCanvas;
            if (canvas == null) return "任务面板未激活";

            string content = "";
            switch (GlobalDataMgr.GetCurrentTaskType())
            {
                case TaskType.Purpose:
                    content = SafePanelContent(canvas.FindPanel<TaskPurposePanel>());
                    break;
                case TaskType.Equipment:
                    content = SafePanelContent(canvas.FindPanel<TaskEquipmentPanel>());
                    break;
                case TaskType.Principle:
                    content = SafePanelContent(canvas.FindPanel<TaskPrinciplePanel>());
                    break;
                case TaskType.Info:
                    content = SafePanelContent(canvas.FindPanel<TaskInfoPanel>());
                    break;
                case TaskType.Structure:
                    content = SafePanelContent(canvas.FindPanel<TaskStructurePanel>());
                    break;
                case TaskType.Inspection:
                    content = SafePanelContent(canvas.FindPanel<TaskInspectionPanel>());
                    break;
                case TaskType.LineConnection:
                    content = SafePanelContent(canvas.FindPanel<TaskLineConnectionPanel>());
                    if (!string.IsNullOrEmpty(content))
                    {
                        string tips = SafeTipsText(canvas);
                        if (!string.IsNullOrEmpty(tips)) content += "当前操作提示" + tips;
                    }
                    break;
                case TaskType.Training:
                    content = SafePanelContent(canvas.FindPanel<TaskTrainingPanel>());
                    if (!string.IsNullOrEmpty(content))
                    {
                        string tips = SafeTipsText(canvas);
                        if (!string.IsNullOrEmpty(tips)) content += "当前操作提示" + tips;
                    }
                    break;
                case TaskType.Test:
                    content = SafePanelContent(canvas.FindPanel<TaskTestPanel>());
                    break;
                case TaskType.Exam:
                    // WHY: 考核面板不是 TaskPanelBase（没有 GetPanelContent 契约），故用固定文案
                    content = "考核：逐题作答单选题，作答即时反馈对错，答对后自动进入下一题";
                    break;
                default:
                    // WHY: 无匹配（如 TaskType.None）不再取 TaskDefaultPanel —— 那是"先建再描述"的副作用来源；
                    //      内容留空，由方法末尾统一降级成"任务面板暂无内容"
                    break;
            }
            return string.IsNullOrEmpty(content) ? "任务面板暂无内容" : content;
        }

        /// <summary>安全取任务面板内容：面板不存在或返回 null/空/异常时降级为空串，不抛异常。</summary>
        static string SafePanelContent(TaskPanelBase panel)
        {
            if (panel == null) return "";
            string text = null;
            try
            {
                text = panel.GetPanelContent();
            }
            catch (System.Exception)
            {
                return "";
            }
            return text ?? "";
        }

        /// <summary>安全取 Tips 面板文本：面板不存在或异常时降级为空串（只查不建，见 TaskPanelDescription 的 WHY）。</summary>
        static string SafeTipsText(CanvasBase canvas)
        {
            if (canvas == null) return "";
            TipsPanel tips = null;
            try
            {
                tips = canvas.FindPanel<TipsPanel>();
            }
            catch (System.Exception)
            {
                return "";
            }
            if (tips == null) return "";
            try
            {
                return tips.GetTipsText() ?? "";
            }
            catch (System.Exception)
            {
                return "";
            }
        }
        #endregion
    }
}
