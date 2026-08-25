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
    /// <summary>
    /// UI 管理器：Canvas 注册表 + 状态事件驱动重建。
    /// Canvas 常驻（Awake 注册 / OnDestroy 注销），状态变化时由事件驱动
    /// 清理子物体并重建面板。Controller ↔ Canvas 通过本注册表解耦。
    /// </summary>
    public class GlobalUIMgr : SingletonGlobalMgr<GlobalUIMgr>
    {
        #region 参数
        [SerializeField] Models.PlayMode playMode = Models.PlayMode.Debug;
        Dictionary<string, CanvasBase> canvasDict = new Dictionary<string, CanvasBase>();
        SceneState m_CurrentState = SceneState.Setup;
        /// <summary>当前任务类型（由 TaskTypeChangeEventData 实时更新, 供 AI 上下文描述使用）。</summary>
        TaskType m_CurrentTaskType = TaskType.None;
        CanvasBase m_ActiveCanvas;
        /// <summary>进行中的状态/任务切换协程（用于连续切换时取消上一次，避免动画叠加）。</summary>
        Coroutine m_SwitchCoroutine;

        [Header("初始状态"), Tooltip("UI 就绪后自动发布的初始 SceneState（状态系统落地前引导）")]
        [SerializeField] SceneState m_InitialState = SceneState.Start;
        // 已由 m_CurrentTaskType（实时更新的当前任务类型）取代，保留仅用于 Inspector 兼容/初始引导
        [SerializeField] TaskType m_InitialTaskType = TaskType.None;
        bool m_InitialStatePublished = false;


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

        /// <summary>获取当前 SceneState（导航状态机的当前态，供退出/返回等逻辑判断）。未就绪返回 Setup。</summary>
        public static SceneState GetCurrentState()
        {
            if (!Exists || Instance == null) return SceneState.Setup;
            return Instance.m_CurrentState;
        }

        /// <summary>获取当前任务类型（供返回文案等使用）。未就绪返回 None。</summary>
        public static TaskType GetCurrentTaskType()
        {
            if (!Exists || Instance == null) return TaskType.None;
            return Instance.m_CurrentTaskType;
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
                // 跳过未激活的 Canvas，避免在隐藏 Canvas 下创建面板
                if (!canvas.isActiveAndEnabled) continue;
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
        /// <summary>SceneState 变化：带淡入淡出过渡切换到目标 Canvas。</summary>
        void OnSceneStateChanged(SceneStateChangeEventData e)
        {
            SwitchToState(e.State, TaskType.None);
        }

        /// <summary>登录成功：进入 Menu 状态（LoginSuccessEvent 唯一监听方，常驻订阅，不会随 Canvas 销毁）。</summary>
        void OnLoginSuccess(LoginSuccessEvent e)
        {
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(SceneState.Menu));
            Log.Info("[GlobalUIMgr] 登录成功，进入菜单界面");
        }

        /// <summary>TaskType 变化：记录当前任务类型，并在当前 Canvas 内带过渡重建任务面板。</summary>
        void OnTaskTypeChanged(TaskTypeChangeEventData e)
        {
            m_CurrentTaskType = e.TaskType;
            if (m_ActiveCanvas == null) return;
            SwitchToState(m_CurrentState, e.TaskType);
        }

        /// <summary>
        /// 状态/任务切换入口：以「淡出当前 → 重建目标 → 淡入目标」的动画过渡方式切换。
        /// 同一时间只保留一个切换协程，连续切换会取消上一次。
        /// </summary>
        void SwitchToState(SceneState state, TaskType taskType)
        {
            var all = new List<CanvasBase>(canvasDict.Values);
            var target = all.Find(c => c.MatchesState(state));
            if (target == null) return; // 无对应 Canvas 的状态，不处理

            if (m_SwitchCoroutine != null)
            {
                StopCoroutine(m_SwitchCoroutine);
                m_SwitchCoroutine = null;
            }
            m_SwitchCoroutine = StartCoroutine(SwitchToStateCoroutine(target, state, taskType, all));
        }

        IEnumerator SwitchToStateCoroutine(CanvasBase target, SceneState state, TaskType taskType, List<CanvasBase> all)
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
            target.Init(state, taskType);
            target.SetUIActive(true);

            m_SwitchCoroutine = null;
        }

        /// <summary>UI 就绪（首个 Canvas 注册）后，延迟一帧发布初始状态，触发首次重建。</summary>
        void TryPublishInitialState()
        {
            if (m_InitialStatePublished) return;
            m_InitialStatePublished = true;
            StartCoroutine(PublishInitialState());
        }

        IEnumerator PublishInitialState()
        {
            yield return null; // 等所有 Canvas 注册完成再发布
            EventBus<SceneStateChangeEventData>.Publish(new SceneStateChangeEventData(m_InitialState));
        }
        #endregion

        #region 界面内容注入AI 提示词
/*
        这里主要是UI界面的数据注入到AI的每轮提示词中，让AI明确当前用户在干嘛
*/
        public static string CurrentStateDescription()
        {
            if (!Exists || Instance == null) return "";

            var mgr = Instance;
            string result = "";
            result += "当前处于：" + SceneStateDescription(mgr.m_CurrentState) + "\n";

            // 仅在进入功能场景(UI/漫游)时才附加任务上下文
            if (mgr.m_CurrentState == SceneState.UI || mgr.m_CurrentState == SceneState.Roaming)
            {
                result += "当前任务：" + mgr.m_CurrentTaskType.ToString() + "\n";
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

        static string TaskPanelDescription()
        {
            var canvas = Instance.m_ActiveCanvas;
            if (canvas == null) return "任务面板未激活";

            string content = "";
            switch (Instance.m_CurrentTaskType)
            {
                case TaskType.Purpose:
                    content = SafePanelContent(canvas.GetPanel<TaskPurposePanel>());
                    break;
                case TaskType.Equipment:
                    content = SafePanelContent(canvas.GetPanel<TaskEquipmentPanel>());
                    break;
                case TaskType.Principle:
                    content = SafePanelContent(canvas.GetPanel<TaskPrinciplePanel>());
                    break;
                case TaskType.LineConnection:
                    content = SafePanelContent(canvas.GetPanel<TaskLineConnectionPanel>());
                    if (!string.IsNullOrEmpty(content))
                    {
                        string tips = SafeTipsText(canvas);
                        if (!string.IsNullOrEmpty(tips)) content += "当前操作提示" + tips;
                    }
                    break;
                case TaskType.Training:
                    content = SafePanelContent(canvas.GetPanel<TaskTrainingPanel>());
                    if (!string.IsNullOrEmpty(content))
                    {
                        string tips = SafeTipsText(canvas);
                        if (!string.IsNullOrEmpty(tips)) content += "当前操作提示" + tips;
                    }
                    break;
                case TaskType.Test:
                    content = SafePanelContent(canvas.GetPanel<TaskTestPanel>());
                    break;
                default:
                    content = SafePanelContent(canvas.GetPanel<TaskDefaultPanel>());
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

        /// <summary>安全取 Tips 面板文本：面板不存在或异常时降级为空串。</summary>
        static string SafeTipsText(CanvasBase canvas)
        {
            if (canvas == null) return "";
            TipsPanel tips = null;
            try
            {
                tips = canvas.GetPanel<TipsPanel>();
            }
            catch (System.Exception)
            {
                return "";
            }
            if (tips == null) return "";
            try
            {
                return tips.GetText() ?? "";
            }
            catch (System.Exception)
            {
                return "";
            }
        }
        #endregion
    }
}
