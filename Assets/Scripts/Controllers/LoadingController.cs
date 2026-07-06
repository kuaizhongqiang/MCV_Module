using MCV_Module.Event;
using MCV_Module.UI.Panels;

namespace MCV_Module.Controller
{
    public class LoadingController : ControllerBase<LoadingPanel>
    {
        protected override void OnViewBound()
        {
            // LoadingPanel 通过 EventBus 自驱动，Controller 负责监听加载完成事件
            EventBus<SceneLoadedEvent>.Subscribe(OnSceneLoaded);
        }

        private void OnSceneLoaded(SceneLoadedEvent evt)
        {
            // TODO: M3 填入 —— 场景加载完成后的 UI 刷新逻辑
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus<SceneLoadedEvent>.Unsubscribe(OnSceneLoaded);
        }
    }
}
