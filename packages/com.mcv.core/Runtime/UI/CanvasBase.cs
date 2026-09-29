using System.Collections.Generic;
using MCV_Module.Utils;
using MCV_Module.UI.Tools;
using MCV_Module.Managers;
using MCV_Module.Models;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI
{
    [RequireComponent(typeof(Canvas))]
    /// <summary>画布基类：向 GlobalUIMgr 注册自身、持有面板注册表，状态/任务变化时清空并重建子物体。</summary>
    public abstract class CanvasBase : UIBase
    {
        [Header("所属状态"), Tooltip("该 Canvas 服务的 SceneState，用于状态切换时定位")]
        [SerializeField] protected SceneState m_SceneState = SceneState.UI;

        protected Canvas canvas;
        protected Dictionary<string, PanelBase> panels = new Dictionary<string, PanelBase>();

        public SceneState CanvasState => m_SceneState;
        public bool MatchesState(SceneState state) => m_SceneState == state;

        // WHY: 加载遮挡层必须活过状态切换，否则切换时遮罩被拔掉，加载一快就会闪一下
        /// <summary>是否常驻画布：不参与状态切换（不淡出/不 ClearPanels/不被选成目标）。</summary>
        public virtual bool IsPersistent => false;

        protected override void Awake()
        {
            base.Awake();
            canvas = GetComponent<Canvas>();
            ClearChildren(transform);
            // Canvas 常驻：仅注册一次，不随显示/隐藏注销
            GlobalUIMgr.RegisterCanvas(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (GlobalUIMgr.Exists)
            {
                GlobalUIMgr.UnregisterCanvas(this);
            }
        }

        // WHY: Canvas 本体不销毁、只重建子物体；面板注册表必须一起清空，否则留下已销毁面板的过期引用。
        /// <summary>清空面板：销毁所有子物体并清空面板注册表。</summary>
        public void ClearPanels()
        {
            ClearChildren(transform);
            panels.Clear();
        }

        /// <summary>清空子物体后重新装配面板（由 GlobalUIMgr 在状态 / 任务类型事件到达时调用）。</summary>
        public void Rebuild()
        {
            ClearPanels();
            OnRebuild();
        }

        /// <summary>子类实现：重建本 Canvas 的面板（无需再判 SceneState，切换时由 GlobalUIMgr 定位）。</summary>
        protected virtual void OnRebuild() { }

        public void RegisterPanel(PanelBase panel)
        {
            string panelName = panel.GetType().Name;
            if (!panels.ContainsKey(panelName))
            {
                panels.Add(panelName, panel);
                panel.SetCanvas(this);
            }
        }

        public void UnregisterPanel(PanelBase panel)
        {
            string panelName = panel.GetType().Name;
            if (panels.ContainsKey(panelName))
            {
                panels.Remove(panelName);
            }
        }

        public T GetPanel<T> () where T : PanelBase
        {
            string panelName = typeof(T).Name;
            if (panels.ContainsKey(panelName))
            {
                return panels[panelName] as T;
            }
            return CreatePanel(panelName) as T;
        }

        // WHY: 收起/收尾路径必须用它 —— GetPanel 会先建一个再立刻关掉，新建实例的 Awake 会先亮一帧（又一次闪）
        /// <summary>取已注册的面板，不存在时返回 null（不创建）。</summary>
        public T FindPanel<T>() where T : PanelBase
        {
            string panelName = typeof(T).Name;
            if (panels.TryGetValue(panelName, out PanelBase panel))
            {
                return panel as T;
            }
            return null;
        }

        // WHY: 面板 prefab 自 B1.5 起住在 UI 全局包（bundle UI/ui，id = ui_{类名}），不再走 Resources；
        // 取件是同步的，依赖 Setup 阶段已预加载整个 UI 包（失败日志在 UIPrefabUtil 里统一打）。
        PanelBase CreatePanel(string panelName)
        {
            GameObject prefab = UIPrefabUtil.Get(panelName);
            if (prefab == null)
            {
                return null;
            }
            GameObject go = Instantiate(prefab, transform);
            go.name = panelName;
            PanelBase panel = go.GetComponent<PanelBase>();

            RegisterPanel(panel);
            return panel;
        }

        /// <summary>创建并注册指定类型的面板（供子类 OnRebuild 使用）。</summary>
        protected T CreatePanel<T>() where T : PanelBase
        {
            return CreatePanel(typeof(T).Name) as T;
        }
        // WHY: 这是**不带等一帧**的同步版，只在清楚不涉及 TextComponent 装配时用；面板侧一律走
        //      PanelBase.RequestLayoutRebuild（等一帧 + 按深度自下而上，见 UI/Tools/UILayoutRebuilder）——
        //      同帧刷会量到 TMP 形态下还没装配完的空文本，表现为"第一次打开排版不对、第二次才对"。
        public void LayoutRebuild()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }

    }
}
