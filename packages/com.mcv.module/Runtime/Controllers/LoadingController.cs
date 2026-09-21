using System.Collections;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.UI.Panels;
using MCV_Module.UI.UICanvas;
using UnityEngine;

namespace MCV_Module.Controllers
{
    /// <summary>
    /// 加载遮挡控制器：编排 LoadingPanel（View）在 AA 包加载 / 场景切换时遮挡屏幕。
    /// 事件驱动：
    ///   SceneLoadingEvent（场景开始加载，Progress=0）→ 显示面板 + 更新进度
    ///   SceneLoadedEvent（场景加载完成）            → 隐藏面板
    ///
    /// **遮挡层住在常驻 LoadingCanvas 上**（不是状态画布）：状态 / 任务切换会
    /// 「淡出画布 → ClearPanels 销毁全部子物体 → Rebuild → 淡入」，
    /// 挂在状态画布下的遮挡层会被连根拔掉/跟着父画布淡出，加载一快就闪。
    /// LoadingCanvas 是 IsPersistent 画布 + sortingOrder 1000，不参与切换且压在所有画布之上。
    /// 因此本类不能只依赖 ControllerBase 绑定的 View —— 需要时按常驻画布懒加载。
    ///
    /// **最短显示时长（minShowDuration，默认 1.2s）**：加载命中缓存时可能一两帧就跑完，
    /// 遮挡层一闪而过，观感上就是画面在闪。约定：**每次 loading 的显示阶段至少 minShowDuration 秒**——
    ///   - 真实加载比它快 → 面板留着撑满，再收起；
    ///   - 真实加载比它慢 → 不做任何额外等待，加载完成立即收起（进度完全按真实加载推进）。
    /// 计时用 unscaledTime，暂停 / 慢动作（timeScale=0）下同样有效。
    /// </summary>
    public class LoadingController : ControllerBase<LoadingPanel>
    {
        [SerializeField, Tooltip("遮挡层最短显示时长（秒）：加载再快也要撑满，避免一闪而过；真实加载更慢时不额外等待")]
        float minShowDuration = 1.2f;

        /// <summary>
        /// 遮挡层当前是否「应该显示」：从本次 loading 的第一条事件起为 true，
        /// 直到真正收起为止（含撑满最短时长的等待窗口）—— Canvas 在等待窗口内重建时也要把遮挡层还原成显示。
        /// </summary>
        bool m_IsLoading;
        /// <summary>最近一次收到的加载进度，用于面板重建后恢复进度显示。</summary>
        float m_LastProgress;
        /// <summary>本次显示阶段的起点（unscaledTime），最短显示时长从这里算起。</summary>
        float m_ShowStartTime;
        /// <summary>
        /// 加载会话号：每开始一轮新 loading +1。延时收起靠它判断"等待期间是否已经换了一轮"——
        /// **不能用 m_IsLoading 判断**：撑满最短时长的窗口内 m_IsLoading 故意保持 true
        /// （这样窗口内 Canvas 重建也能把遮挡层还原成显示），拿它当"新一轮开始"的判据会让延时收起永远自杀、遮挡层再也不卸载。
        /// </summary>
        int m_SessionId;
        /// <summary>「等最短时长到点」的延时收起协程；新 loading 开始或已收面板时作废。</summary>
        Coroutine m_DelayHideCoroutine;

        protected override void Awake()
        {
            base.Awake(); // 注册自身（GlobalControllerMgr）
            EventBus<SceneLoadingEvent>.Subscribe(OnSceneLoading);
            EventBus<SceneLoadedEvent>.Subscribe(OnSceneLoaded);
        }

        protected override void OnViewBound()
        {
            // 每次绑定全新面板实例：按当前加载状态复位，避免「显示中的遮挡层被刚创建的面板回退成隐藏」
            if (View == null) return;
            View.SetUIActiveImmediately(m_IsLoading);
            if (m_IsLoading) View.SetProgress(m_LastProgress);
        }

        /// <summary>
        /// 取遮挡面板：优先用已绑定的 View（被销毁时是 Unity 伪 null，会自动继续往下找）；
        /// 否则在**常驻 LoadingCanvas** 上取 —— 遮挡层不能再挂在状态画布下（那种画布在状态切换时
        /// 会被淡出 + ClearPanels 销毁全部子物体，遮罩会被连根拔掉，加载一快就闪一下）。
        ///
        /// createIfMissing=false 供收起路径使用：只找现成的面板，**绝不为「关掉它」而新建一个** ——
        /// 新建实例的 Awake（isActiveOnInstance=1）会先亮一帧，又是一次闪。
        /// </summary>
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
            // 新一轮加载：作废上一轮「撑满最短时长」的延时收起，
            // 否则它会在新一轮显示中途补一次收起（面板刚显示就被关掉）。
            CancelDelayHide();

            // 会话起点 = 上一轮已收尾（m_IsLoading 为 false）时的第一条事件，
            // 最短显示时长从这一帧算起（同一帧下面就会显示面板）。
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

            // 只在会话首条事件执行显示（文案用通用措辞：同一块遮挡面板既服务场景加载，
            // 也服务内容包（一 ProjectClip 一包）加载）；后续进度事件只刷进度，不重播渐显。
            if (newSession)
            {
                panel.SetUIActive(true);
                panel.Init(null, "加载中", "正在加载资源，请稍候...");
            }
            panel.SetProgress(e.Progress);
        }

        /// <summary>场景加载完成：收起遮挡面板（若没撑满最短显示时长则延后收起）。</summary>
        void OnSceneLoaded(SceneLoadedEvent e)
        {
            var panel = ResolvePanel(false);   // 收起路径只找现成的，不新建

            // 面板不存在（本就没显示过遮挡）/ 已失活 → 不需要收起动画，本次 loading 直接结束。
            // 失活判断不能省：UIBase.SetUIActive(false) 在 inactive 的 GameObject 上 StartCoroutine 会抛
            // "Coroutine couldn't be started because the gameObject is inactive"。
            if (panel == null || !panel.gameObject.activeInHierarchy)
            {
                m_IsLoading = false;
                m_LastProgress = 0f;
                return;
            }

            float remain = Mathf.Max(0f, minShowDuration) - (Time.unscaledTime - m_ShowStartTime);
            if (remain > 0f)
            {
                // 加载比最短时长快：先让面板留着（进度停在最后的值），撑满 minShowDuration 再收
                CancelDelayHide();
                m_DelayHideCoroutine = StartCoroutine(DelayHideRoutine(m_SessionId, remain));
                return;
            }

            HidePanel(panel);
        }

        /// <summary>
        /// 撑满最短显示时长后再收起面板。用 realtime 等待：loading 期间可能被 timeScale=0 暂停 / 慢动作，
        /// 遮挡层的展示时长不该被时间缩放影响。
        /// </summary>
        IEnumerator DelayHideRoutine(int sessionId, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            m_DelayHideCoroutine = null;

            // 等待期间已经换了一轮 loading（新一轮的 OnSceneLoading 会 CancelDelayHide 停掉本协程，
            // 这里只是兜底）：本轮不收了，交给新一轮显示。
            // 判据必须是会话号，不能是 m_IsLoading —— 窗口内它一直为 true（见字段注释）。
            if (sessionId != m_SessionId) yield break;

            // 常驻画布上的实例不会被状态切换销毁，但这里仍按「只找现成的」取，绝不新建
            HidePanel(ResolvePanel(false));
        }

        /// <summary>作废等待中的延时收起（新 loading 开始 / 控制器销毁时调用）。</summary>
        void CancelDelayHide()
        {
            if (m_DelayHideCoroutine == null) return;
            StopCoroutine(m_DelayHideCoroutine);
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

        protected override void OnDestroy()
        {
            CancelDelayHide();
            EventBus<SceneLoadingEvent>.Unsubscribe(OnSceneLoading);
            EventBus<SceneLoadedEvent>.Unsubscribe(OnSceneLoaded);
            base.OnDestroy();
        }
    }
}
