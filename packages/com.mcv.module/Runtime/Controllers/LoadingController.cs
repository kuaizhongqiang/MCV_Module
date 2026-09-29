using System.Collections;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.UI.Panels;
using MCV_Module.UI.UICanvas;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Controllers
{
    // WHY: 遮挡层必须住在常驻 LoadingCanvas(IsPersistent + sortingOrder 1000) —— 挂在状态画布下会被「淡出 → ClearPanels → Rebuild → 淡入」连根拔掉, 加载一快就闪; 故不能只依赖 ControllerBase 绑定的 View, 需要时按常驻画布懒加载
    /// <summary>加载遮挡控制器：编排 LoadingPanel 在 AA 包加载 / 场景切换时遮挡屏幕（SceneLoadingEvent 显示 + SceneLoadedEvent 收起，最短显示时长取面板的 MinShowDuration，unscaledTime 计时）。</summary>
    public class LoadingController : ControllerBase<LoadingPanel>
    {
        // WHY: minShowDuration 已迁到 LoadingPanel（属面板自身的显示表现，与呼吸曲线/生命周期同层）；
        //       控制器只读面板上的 MinShowDuration，不再自带序列化字段 —— 控制器已不挂场景，序列化字段也无处可存。
        // WHY: 等待窗口内必须保持 true —— Canvas 在窗口内重建时也要把遮挡层还原成显示
        /// <summary>遮挡层当前是否「应该显示」（从本次 loading 第一条事件起，直到真正收起）。</summary>
        bool m_IsLoading;
        /// <summary>最近一次收到的加载进度，用于面板重建后恢复进度显示。</summary>
        float m_LastProgress;
        /// <summary>本次显示阶段的起点（unscaledTime），最短显示时长从这里算起。</summary>
        float m_ShowStartTime;
        // WHY: 延时收起的判据必须用会话号, **不能用 m_IsLoading** —— 窗口内它故意保持 true, 拿它当"新一轮开始"的判据会让延时收起永远自杀、遮挡层再也不卸载
        /// <summary>加载会话号：每开始一轮新 loading +1。</summary>
        int m_SessionId;
        /// <summary>「等最短时长到点」的延时收起协程；新 loading 开始或已收面板时作废。</summary>
        Coroutine m_DelayHideCoroutine;

        public override void OnInit()
        {
            base.OnInit(); // 注册自身（GlobalControllerMgr）
            EventBus<SceneLoadingEvent>.Subscribe(OnSceneLoading);
            EventBus<SceneLoadedEvent>.Subscribe(OnSceneLoaded);
        }

        public override void OnViewBound()
        {
            // 每次绑定全新面板实例：按当前加载状态复位，避免「显示中的遮挡层被刚创建的面板回退成隐藏」
            if (View == null) return;
            View.SetUIActiveImmediately(m_IsLoading);
            if (m_IsLoading) View.SetProgress(m_LastProgress);
        }

        // WHY: 收起路径(createIfMissing=false)只找现成面板, **绝不为「关掉它」新建** —— 新实例 Awake(isActiveOnInstance=1)会先亮一帧, 又是一次闪
        /// <summary>取遮挡面板：优先已绑定的 View，否则在常驻 LoadingCanvas 上按 createIfMissing 取 / 建。</summary>
        LoadingPanel ResolvePanel(bool createIfMissing)
        {
            if (View != null) return View;

            var canvas = createIfMissing ? LoadingCanvas.Ensure() : LoadingCanvas.Find();
            if (canvas == null) return null;

            return createIfMissing ? canvas.GetPanel<LoadingPanel>() : canvas.FindPanel<LoadingPanel>();
        }

        /// <summary>场景开始加载：显示遮挡面板并更新进度。</summary>
        void OnSceneLoading(SceneLoadingEvent e)
        {
            // WHY: 新一轮加载必须作废上一轮的延时收起, 否则它会在新一轮显示中途补一次收起(面板刚显示就被关掉)
            CancelDelayHide();

            // 会话起点 = 上一轮已收尾(m_IsLoading 为 false)时的第一条事件, 最短显示时长从这一帧算起(同一帧下面就会显示面板)
            bool newSession = !m_IsLoading;
            if (newSession)
            {
                m_SessionId++;
                m_ShowStartTime = Time.unscaledTime;
            }

            m_IsLoading = true;
            m_LastProgress = e.Progress;

            var panel = ResolvePanel(true);   // 唯一创建入口：常驻 LoadingCanvas 上懒加载
            if (panel == null)
            {
                // 没有常驻加载画布（单例未就绪等）时静默降级：不遮挡，但流程照走
                return;
            }

            // 只在会话首条事件执行显示(文案用通用措辞: 同一块遮挡面板既服务场景加载也服务内容包加载); 后续进度事件只刷进度, 不重播渐显
            if (newSession)
            {
                panel.SetUIActive(true);
                // WHY: 文案必须走 Lang（key 已在 LanguageDataSO 登记），硬编码中文会让英文态这块永远是中文。
                panel.Init(null, Lang.Get("ui.loading.title"), Lang.Get("ui.loading.content"));
            }
            panel.SetProgress(e.Progress);
        }

        /// <summary>场景加载完成：收起遮挡面板（若没撑满最短显示时长则延后收起）。</summary>
        void OnSceneLoaded(SceneLoadedEvent e)
        {
            var panel = ResolvePanel(false);   // 收起路径只找现成的，不新建

            // WHY: 失活判断不能省 —— 协程已挪到 GlobalControllerMgr，但 SetUIActive(false) 本身在 inactive 的 GameObject 上仍会触发面板内部协程报错，收起路径必须先挡一道
            if (panel == null || !panel.gameObject.activeInHierarchy)
            {
                m_IsLoading = false;
                m_LastProgress = 0f;
                return;
            }

            // WHY: 最短显示时长取自面板（参数已迁到 LoadingPanel），面板缺失时按 0 处理（不等待、立即收）
            float minShow = panel.MinShowDuration;
            float remain = minShow - (Time.unscaledTime - m_ShowStartTime);
            if (remain > 0f)
            {
                // 加载比最短时长快：先让面板留着（进度停在最后的值），撑满 minShow 再收
                CancelDelayHide();
                m_DelayHideCoroutine = Run(DelayHideRoutine(m_SessionId, remain));
                return;
            }

            HidePanel(panel);
        }

        // WHY: 用 realtime 等待 —— loading 期间可能被 timeScale=0 暂停 / 慢动作, 展示时长不该被时间缩放影响
        /// <summary>撑满最短显示时长后再收起面板。</summary>
        IEnumerator DelayHideRoutine(int sessionId, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            m_DelayHideCoroutine = null;

            // WHY: 判据必须是会话号, 不能是 m_IsLoading —— 窗口内它一直为 true(见字段注释); 换轮后本轮不收了, 交给新一轮显示
            if (sessionId != m_SessionId) yield break;

            // 常驻画布上的实例不会被状态切换销毁，但这里仍按「只找现成的」取，绝不新建
            HidePanel(ResolvePanel(false));
        }

        /// <summary>作废等待中的延时收起（新 loading 开始 / 控制器销毁时调用）。</summary>
        void CancelDelayHide()
        {
            if (m_DelayHideCoroutine == null) return;
            Halt(m_DelayHideCoroutine);
            m_DelayHideCoroutine = null;
        }

        /// <summary>真正收起遮挡层：复位加载状态后再播收起动画。</summary>
        void HidePanel(LoadingPanel panel)
        {
            m_IsLoading = false;
            m_LastProgress = 0f;

            if (panel == null) return;
            if (!panel.gameObject.activeInHierarchy) return;

            panel.SetUIActive(false);
        }

        public override void OnDispose()
        {
            CancelDelayHide();
            EventBus<SceneLoadingEvent>.Unsubscribe(OnSceneLoading);
            EventBus<SceneLoadedEvent>.Unsubscribe(OnSceneLoaded);
            base.OnDispose();
        }
    }
}
