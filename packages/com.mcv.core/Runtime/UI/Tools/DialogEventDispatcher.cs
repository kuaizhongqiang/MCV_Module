using MCV_Module.Managers;
using MCV_Module.Utils;
using MCV_Module.UI.Panels;

namespace MCV_Module.Event
{
    // WHY: 任何系统发布 DialogRequestEvent 都从这里统一收口；框架启动 Initialize 一次、场景切换/退出时必须 Shutdown，否则静态处理器会悬挂。
    /// <summary>DialogRequestEvent 集中式分发器：GlobalUIMgr → 激活 Canvas → GetPanel&lt;DialogPanel&gt; → Show。</summary>
    public static class DialogEventDispatcher
    {
        static bool s_Initialized;

        /// <summary>订阅 DialogRequestEvent，开始分发对话框显示请求。重复调用幂等。</summary>
        public static void Initialize()
        {
            if (s_Initialized) return;
            EventBus<DialogRequestEvent>.Subscribe(OnDialogRequested);
            s_Initialized = true;
        }

        /// <summary>取消订阅，停止分发。场景切换 / 应用退出时调用，防止悬挂订阅。</summary>
        public static void Shutdown()
        {
            if (!s_Initialized) return;
            EventBus<DialogRequestEvent>.Unsubscribe(OnDialogRequested);
            s_Initialized = false;
        }

        /// <summary>是否已完成初始化（供外部判断）。</summary>
        public static bool IsInitialized => s_Initialized;

        /// <summary>处理入口：GlobalUIMgr → 激活 Canvas → GetPanel&lt;DialogPanel&gt; → Show；任一环缺失都安全降级、不抛异常。</summary>
        static void OnDialogRequested(DialogRequestEvent request)
        {
            if (request == null) return;

            // 1. 找到 GlobalUIMgr（单例未就绪则忽略，避免空引用）
            if (!GlobalUIMgr.Exists || GlobalUIMgr.Instance == null) return;

            // 2. 找到当前激活的 Canvas
            var canvas = GlobalUIMgr.GetActiveCanvas();
            if (canvas == null) return;

            // 3. 在激活 Canvas 中获取（懒加载创建）DialogPanel 并显示
            var panel = canvas.GetPanel<DialogPanel>();
            if (panel == null)
            {
                Log.Error("[DialogEventDispatcher] 无法创建 DialogPanel —— 请确认 UI 包里有 ui_DialogPanel（跑 MCV Build/UI prefab AB，且 Setup 的 UI 包预加载成功）");
                return;
            }

            panel.Show(request);
        }
    }
}
