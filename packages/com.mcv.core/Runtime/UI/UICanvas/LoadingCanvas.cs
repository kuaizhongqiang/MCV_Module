using MCV_Module.Managers;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.UICanvas
{
    // WHY: 遮挡层若挂在状态画布下，会被 SwitchToStateCoroutine 的淡出 + ClearPanels 连根拔掉，加载一快就闪一下；IsPersistent + sortingOrder 1000 让它活过切换并压在最上层
    /// <summary>常驻加载画布：只承载 LoadingPanel（加载遮挡层），不参与状态切换。</summary>
    public class LoadingCanvas : CanvasBase
    {
        /// <summary>遮挡层排序值：场景里的状态画布与面板内嵌画布都是 0，这里取一个足够大的值压在最上面。</summary>
        public const int SortingOrder = 1000;

        /// <summary>常驻画布：不参与状态切换。</summary>
        public override bool IsPersistent => true;

        /// <summary>常驻画布不参与重建（它只有遮挡层一块面板，由 LoadingController 按加载事件驱动）。</summary>
        protected override void OnRebuild() { }

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

            // WHY: 必须用带 RectTransform 的构造，不能先建普通 Transform 再转
            var go = new GameObject(nameof(LoadingCanvas), typeof(RectTransform));
            go.SetActive(false);                    // WHY: 装配期间保持关闭，避免半成品 Canvas 先注册 / 先渲染

            // WHY: 遮挡层是全局基础设施，要跨场景常驻，不能随漫游场景卸载 / 单场景切换被销毁
            DontDestroyOnLoad(go);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;     // 覆盖全部状态画布

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);   // 与 1_Content 里的画布一致，保证遮挡层比例不变
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // WHY: 没有 Raycaster 就挡不住点击——事件系统按画布做射线检测，会直接穿到下面的状态画布
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<CanvasGroup>();         // WHY: UIBase 依赖（Awake 里 GetComponent<CanvasGroup>）

            go.SetActive(true);

            // WHY: 业务脚本最后加——它的 Awake 要取 Canvas / CanvasGroup，此时才都已就位
            var loadingCanvas = go.AddComponent<LoadingCanvas>();
            Log.Info($"[LoadingCanvas] 场景中未摆放常驻加载画布，已运行时补建（sortingOrder={SortingOrder}）");
            return loadingCanvas;
        }
    }
}
