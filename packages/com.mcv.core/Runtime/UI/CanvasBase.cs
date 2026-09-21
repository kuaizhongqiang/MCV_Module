using System.Collections.Generic;
using MCV_Module.Utils;
using MCV_Module.Managers;
using MCV_Module.Models;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI
{
    [RequireComponent(typeof(Canvas))]
    public abstract class CanvasBase : UIBase
    {
        [Header("所属状态"), Tooltip("该 Canvas 服务的 SceneState，用于状态切换时定位")]
        [SerializeField] protected SceneState m_SceneState = SceneState.UI;

        protected Canvas canvas;
        protected Dictionary<string, PanelBase> panels = new Dictionary<string, PanelBase>();

        public SceneState CanvasState => m_SceneState;
        public bool MatchesState(SceneState state) => m_SceneState == state;

        /// <summary>
        /// 是否常驻画布：常驻画布**不参与状态切换**（不被淡出/隐藏，也不参与 ClearPanels + Rebuild），
        /// 也不会被选成切换目标。用于加载遮挡层这类「跨状态覆盖物」——它必须活过状态切换，
        /// 否则切换时遮罩被连根拔掉/跟着父画布淡出，加载一快就看到闪一下。
        /// </summary>
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

        /// <summary>
        /// 清空面板：销毁所有子物体 + 清空面板注册表（防已销毁面板的过期引用）。
        /// Canvas 本体不销毁，只重建子物体。
        /// </summary>
        public void ClearPanels()
        {
            ClearChildren(transform);
            panels.Clear();
        }

        /// <summary>
        /// 按状态初始化：清空后重建面板。由 GlobalUIMgr 响应状态事件时调用。
        /// </summary>
        public void Init(SceneState state, TaskType taskType)
        {
            ClearPanels();
            OnRebuild(state, taskType);
        }

        /// <summary>子类实现：按当前状态重建本 Canvas 的面板。</summary>
        protected virtual void OnRebuild(SceneState state, TaskType taskType) { }

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

        /// <summary>
        /// 取已注册的面板，不存在时返回 null（**不创建**）。
        /// 收起 / 收尾路径要用这个：用 <see cref="GetPanel{T}"/> 会在面板不存在时先建一个再立刻关掉，
        /// 新建实例的 Awake（isActiveOnInstance）会先亮一帧 —— 又是一次闪。
        /// </summary>
        public T FindPanel<T>() where T : PanelBase
        {
            string panelName = typeof(T).Name;
            if (panels.TryGetValue(panelName, out PanelBase panel))
            {
                return panel as T;
            }
            return null;
        }

        PanelBase CreatePanel(string panelName)
        {
            string panelPath = "UI/" + panelName;
            GameObject prefab = Resources.Load<GameObject>(panelPath);
            if (prefab == null)
            {
                Log.Error($"[CanvasBase] 面板 Prefab 不存在：Resources/{panelPath}（请用 MCV/创建/UI Panel 生成器生成）");
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
        public void LayoutRebuild()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }

    }
}
