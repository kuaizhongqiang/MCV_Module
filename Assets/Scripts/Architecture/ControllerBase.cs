
using System.Collections;
using MCV_Module.GlobalManager;
using MCV_Module.UI;
using UnityEngine;

namespace MCV_Module.Controller
{
    /// <summary>
    /// Controller 基类，负责调度逻辑与数据转换。
    /// 挂载在场景空物体上，通过泛型 TView 绑定对应的 View。
    /// 数据流：Controller → View（单向），Controller → Service.Instance（读取数据）。
    /// 实现 IController 接口以便 GlobalControllerMgr 统一注册。
    /// </summary>
    public abstract class ControllerBase<TView> : MonoBehaviour, IController where TView : PanelBase
    {
        public string ControllerName => GetType().Name;

        protected TView View { get; private set; }

        /// <summary>
        /// 等待 View 就绪后绑定，子类可重写 BindView 改变绑定方式。
        /// </summary>
        protected IEnumerator Start()
        {
            yield return BindView();
            OnViewBound();
            RegisterSelf();
        }

        /// <summary>
        /// 绑定 View 的协程，遍历所有已加载场景搜索 CanvasBase。
        /// Controller 可能在 1_Content，UI 在 2_UIScene，不能用 GetActiveScene。
        /// </summary>
        protected virtual IEnumerator BindView()
        {
            View = GlobalUiMgr.GetPanel<TView>();
            while (View == null)
            {                
                yield return null;
            }
            yield break;
        }

        /// <summary>View 绑定完成后调用，在此注册事件监听。</summary>
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
