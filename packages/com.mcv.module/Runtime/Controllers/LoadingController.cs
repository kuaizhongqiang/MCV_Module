using MCV_Module.Controller;
using MCV_Module.Event;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controllers
{
    /// <summary>
    /// 加载遮挡控制器：编排 LoadingPanel（View）在 AA 包加载 / 场景切换时遮挡屏幕。
    /// 事件驱动：
    ///   SceneLoadingEvent（场景开始加载，Progress=0）→ 显示面板 + 更新进度
    ///   SceneLoadedEvent（场景加载完成）            → 隐藏面板
    /// </summary>
    public class LoadingController : ControllerBase<LoadingPanel>
    {
        protected override void Awake()
        {
            base.Awake(); // 注册自身（GlobalControllerMgr）
            EventBus<SceneLoadingEvent>.Subscribe(OnSceneLoading);
            EventBus<SceneLoadedEvent>.Subscribe(OnSceneLoaded);
        }

        protected override void OnViewBound()
        {
            // 每次绑定全新面板实例：默认隐藏遮挡层，等待场景加载事件触发显示
            View.SetUIActiveImmediately(false);
        }

        /// <summary>场景开始加载：显示遮挡面板并更新进度。</summary>
        void OnSceneLoading(SceneLoadingEvent e)
        {
            if (View == null) return;

            // 首次进入（Progress=0）才执行显示，避免完成帧（Progress=1）重复触发渐显
            if (e.Progress <= 0f)
            {
                View.SetUIActive(true);
                View.Init(null, "场景加载中", "正在加载资源，请稍候...");
            }
            View.SetProgress(e.Progress);
        }

        /// <summary>场景加载完成：隐藏遮挡面板。</summary>
        void OnSceneLoaded(SceneLoadedEvent e)
        {
            if (View == null) return;
            View.SetUIActive(false);
        }

        protected override void OnDestroy()
        {
            EventBus<SceneLoadingEvent>.Unsubscribe(OnSceneLoading);
            EventBus<SceneLoadedEvent>.Unsubscribe(OnSceneLoaded);
            base.OnDestroy();
        }
    }
}
