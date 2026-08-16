
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.UI;
using UnityEngine;

namespace MCV_Module.Controller
{
    /// <summary>
    /// Controller 基类，负责调度逻辑与数据转换。
    /// Controller 常驻（挂 1_Content 的 ControllerRoot），跨状态/场景切换不销毁。
    ///
    /// 绑定机制：面板（View）由 Canvas 按状态重建，面板生命周期（Start）按
    /// 1:1 名字约定（TitlePanel → TitleController）找到本 Controller 并调用 Bind。
    /// 因此每次 Canvas 重建都会重新绑定到全新面板实例，无需轮询或事件撮合。
    ///
    /// 数据流：Controller → View（单向），Controller → Service.Instance（读取数据）。
    /// 实现 IController 接口以便 GlobalControllerMgr 统一注册。
    /// </summary>
    public abstract class ControllerBase<TView> : MonoBehaviour, IController where TView : PanelBase
    {
        public string ControllerName => GetType().Name;

        protected TView View { get; private set; }

        /// <summary>
        /// 提前注册，确保面板生命周期查找 Controller 时已可寻。
        /// </summary>
        protected virtual void Awake()
        {
            RegisterSelf();
        }

        /// <summary>
        /// 由面板生命周期调用（1:1 名字约定）。每次 Canvas 重建都会绑定到全新面板实例。
        /// </summary>
        public void Bind(PanelBase panel)
        {
            if (panel is TView view)
            {
                View = view;
                OnViewBound();
            }
            else
            {
                Debug.LogError($"[{ControllerName}] 绑定面板类型不匹配：期望 {typeof(TView).Name}，实际 {panel.GetType().Name}");
            }
        }

        /// <summary>
        /// View 绑定完成后调用，在此注册事件监听。
        /// 每次 Canvas 重建都会重跑 —— 注意先清后加，避免重复订阅。
        /// </summary>
        protected abstract void OnViewBound();

        void IController.OnBindView() { }

        /// <summary>Controller 销毁时调用，在此解绑事件。</summary>
        protected virtual void OnDestroy()
        {
            var mgr = GlobalControllerMgr.Instance;
            if (mgr != null)
                mgr.Unregister(this);
        }

        private void RegisterSelf()
        {
            var mgr = GlobalControllerMgr.Instance;
            if (mgr != null)
                mgr.Register(this);
        }
    }
}
