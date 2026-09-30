using System.Collections;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Utils;
using MCV_Module.UI;
using UnityEngine;

namespace MCV_Module.Controllers
{
    // WHY: Controller 是普通 C# 类（不继承 MonoBehaviour、不挂场景），由 GlobalControllerMgr 在 DelayInit 里统一创建并常驻；
    //      面板仍由 Canvas 重建，按 1:1 名字约定（TitlePanel → TitleController）找到本类调 Bind，每次重建都重绑到全新面板实例，无需轮询或事件撮合。
    // WHY: 生命周期只有两个显式入口 —— OnInit（登记完成后）与 OnDispose（销毁前），不再有 Unity 的 Awake/OnDestroy 回调可依赖。
    /// <summary>Controller 基类：负责调度逻辑与数据转换；单向数据流 Controller → View / Controller → Service.Instance，实现 IController 供 GlobalControllerMgr 统一注册。</summary>
    /// <typeparam name="TView">本控制器绑定的面板类型（1:1）。</typeparam>
    public abstract class ControllerBase<TView> : IController where TView : PanelBase
    {
        // WHY: 每个具体控制器类型各有一份静态构造，登记进 GlobalControllerMgr 的类型表；并发或初始化顺序不确定时由类型表 getter 兜底补齐。
        static ControllerBase()
        {
            GlobalControllerMgr.RegisterControllerType(typeof(ControllerBase<TView>));
        }

        public string ControllerName => GetType().Name;

        protected TView View { get; private set; }

        /// <summary>登记完成后调用一次：常驻订阅写这里（只跑一次）。</summary>
        public virtual void OnInit() { }

        // WHY: 每次 Canvas 重建都会重跑 —— 事件订阅必须先清后加, 避免重复订阅
        /// <summary>View 绑定完成后调用，在此注册面板事件（每次重建都重跑，必须"先退后订"）。</summary>
        public virtual void OnViewBound() { }

        // WHY: 由 GlobalControllerMgr 在停掉本控制器全部协程之后调用；常驻退订写这里。
        /// <summary>销毁前调用一次：常驻订阅退订写这里。</summary>
        public virtual void OnDispose() { }

        // WHY: 由面板生命周期按 1:1 名字约定调用, 每次 Canvas 重建都绑定到全新面板实例
        /// <summary>绑定面板：类型匹配则置 View 并回调 OnViewBound，否则报错。</summary>
        public void Bind(PanelBase panel)
        {
            if (panel is TView view)
            {
                View = view;
                OnViewBound();
            }
            else
            {
                Log.Error($"[{ControllerName}] 绑定面板类型不匹配：期望 {typeof(TView).Name}，实际 {panel.GetType().Name}");
            }
        }

        // WHY: 去 MonoBehaviour 后本类不再持有协程宿主，一律经 GlobalControllerMgr 启动；退出/销毁时由管理器按控制器整组停止，避免协程活过控制器。
        /// <summary>启动一个属于本控制器的协程（宿主是 GlobalControllerMgr，跨场景切换存活）。</summary>
        protected Coroutine Run(IEnumerator routine) => GlobalControllerMgr.Instance?.RunCoroutine(this, routine);

        /// <summary>停止一个由 <see cref="Run"/> 启动的协程。</summary>
        protected void Halt(Coroutine routine) => GlobalControllerMgr.Instance?.StopCoroutine(this, routine);

        /// <summary>停止本控制器启动的全部协程。</summary>
        protected void HaltAll() => GlobalControllerMgr.Instance?.StopAllCoroutines(this);

        // WHY: 面板随 Canvas 重建被销毁，本类不再收到 Unity 回调，重绑后必须由调用方显式清空 View；OnDispose 时也用它放掉引用。
        /// <summary>清空 View 引用（不触发 OnViewBound）。</summary>
        protected void ClearView() => View = null;
    }
}
