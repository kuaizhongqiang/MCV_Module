using System.Collections;
using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Singleton;
using MCV_Module.UI;
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
        Dictionary<string, CanvasBase> canvasDict = new Dictionary<string, CanvasBase>();

        SceneState m_CurrentState = SceneState.Setup;
        CanvasBase m_ActiveCanvas;

        [Header("初始状态"), Tooltip("UI 就绪后自动发布的初始 SceneState（状态系统落地前引导）")]
        [SerializeField] SceneState m_InitialState = SceneState.Start;
        bool m_InitialStatePublished = false;
        #endregion

        #region 生命周期
        protected override IEnumerator DelayInit()
        {
            // 状态事件驱动 Canvas 初始化（强引用，OnDestroy 必须退订）
            EventBus<SceneStateChangeEventData>.Subscribe(OnSceneStateChanged);
            EventBus<TaskTypeChangeEventData>.Subscribe(OnTaskTypeChanged);
            yield return null;
            isInit = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus<SceneStateChangeEventData>.Unsubscribe(OnSceneStateChanged);
            EventBus<TaskTypeChangeEventData>.Unsubscribe(OnTaskTypeChanged);
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
        /// <summary>SceneState 变化：清空全部 Canvas → 激活并重建目标 Canvas。</summary>
        void OnSceneStateChanged(SceneStateChangeEventData e)
        {
            m_CurrentState = e.State;

            var all = new List<CanvasBase>(canvasDict.Values);
            var target = all.Find(c => c.MatchesState(e.State));
            if (target == null) return; // 无对应 Canvas 的状态，不处理

            foreach (var canvas in all)
            {
                canvas.ClearPanels();
                if (canvas != target)
                {
                    canvas.SetUIActiveImmediately(false);
                }
            }

            m_ActiveCanvas = target;
            target.SetUIActiveImmediately(true);
            target.Init(e.State, TaskType.None);
        }

        /// <summary>TaskType 变化：只重建当前 UI Canvas 的任务面板。</summary>
        void OnTaskTypeChanged(TaskTypeChangeEventData e)
        {
            if (m_ActiveCanvas == null) return;
            m_ActiveCanvas.Init(m_CurrentState, e.TaskType);
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
    }
}
