using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.UICanvas
{
    /// <summary>
    /// 常驻加载画布：只承载 LoadingPanel（加载遮挡层），**不参与状态切换**。
    ///
    /// 为什么必须自己一张画布：遮挡层原先挂在状态画布（Start / Login / Menu / Content / Roaming）下，
    /// 而状态 / 任务切换走的是 <c>GlobalUIMgr.SwitchToStateCoroutine</c>：
    ///   淡出当前画布（CanvasGroup.alpha → 0）→ 对所有画布 <c>ClearPanels()</c>（销毁全部子物体）→
    ///   Rebuild → 淡入目标画布。
    /// 于是正在显示的遮罩被连根拔掉（连 alpha 都跟着父画布一起淡到 0），
    /// 加载一旦很快（命中缓存，一两帧就跑完）就看到遮挡层闪一下。
    ///
    /// 放到本画布后：
    ///   - <see cref="IsPersistent"/> = true → GlobalUIMgr 的状态切换直接跳过它（不淡出、不清面板、不会被选成目标）；
    ///   - <see cref="SortingOrder"/> = 1000 → 压在所有状态画布（含面板内嵌画布）之上，遮得住。
    ///
    /// 摆放：可以在场景里直接摆一个挂了本组件的对象（推荐，可预览）；
    /// 没摆时 <see cref="Ensure"/> 会在运行时补一张，参数与场景里的画布一致（1920×1080 + MatchWidthOrHeight）。
    /// </summary>
    public class LoadingCanvas : CanvasBase
    {
        /// <summary>遮挡层排序值：场景里的状态画布与面板内嵌画布都是 0，这里取一个足够大的值压在最上面。</summary>
        public const int SortingOrder = 1000;

        /// <summary>常驻画布：不参与状态切换。</summary>
        public override bool IsPersistent => true;

        /// <summary>
        /// 常驻画布不参与重建（它只有遮挡层一块面板，由 LoadingController 按加载事件驱动）。
        /// 注意保留 CUR 的三参签名（勿照搬 LOW 的无参 OnRebuild）。
        /// </summary>
        protected override void OnRebuild(SceneState state, TaskType taskType) { }

        /// <summary>取现成的常驻加载画布；不存在返回 null（**不创建**）。</summary>
        public static LoadingCanvas Find()
        {
            if (!GlobalUIMgr.Exists) return null;
            return GlobalUIMgr.GetCanvas<LoadingCanvas>();
        }

        /// <summary>取常驻加载画布，没有就运行时补一张（画布参数对齐场景里的状态画布）。</summary>
        public static LoadingCanvas Ensure()
        {
            LoadingCanvas exist = Find();
            if (exist != null) return exist;
            if (!GlobalUIMgr.Exists) return null;   // 单例未就绪（启动早期）→ 调用方静默降级

            // UI 物体的根必须是 RectTransform：用带 RectTransform 的构造，别先建普通 Transform 再转
            var go = new GameObject(nameof(LoadingCanvas), typeof(RectTransform));
            go.SetActive(false);                    // 装配期间保持关闭，避免半成品 Canvas 先注册 / 先渲染

            // 跨场景常驻：遮挡层是全局基础设施，不能随「漫游场景卸载 / 单场景切换」一起被销毁
            DontDestroyOnLoad(go);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;     // 覆盖全部状态画布

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);   // 与 1_Content 里的画布一致，保证遮挡层比例不变
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 没有 Raycaster 就挡不住点击：事件系统按画布做射线检测，会直接穿到下面的状态画布
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<CanvasGroup>();         // UIBase 依赖（Awake 里 GetComponent<CanvasGroup>）

            go.SetActive(true);

            // 业务脚本最后加：它的 Awake 里要取 Canvas / CanvasGroup，此时都已就位
            var loadingCanvas = go.AddComponent<LoadingCanvas>();
            Log.Info($"[LoadingCanvas] 场景中未摆放常驻加载画布，已运行时补建（sortingOrder={SortingOrder}）");
            return loadingCanvas;
        }
    }
}
