using MCV_Module.Managers;
using MCV_Module.UI.Panels;

namespace MCV_Module.Event
{
    /// <summary>
    /// 对话框事件专门处理逻辑 —— 针对 DialogRequestEvent 的集中式分发器。
    ///
    /// 职责：任何系统发布 DialogRequestEvent 时，统一走固定的定位链路把对话框显示出来：
    ///   1. 找到 GlobalUIMgr
    ///   2. 找到当前激活（正在展示）的 Canvas
    ///   3. 在该 Canvas 中 GetPanel&lt;DialogPanel&gt;（不存在则懒加载创建）并调用 Show(request)
    ///
    /// 用法（框架启动时初始化一次）：
    ///   DialogEventDispatcher.Initialize();
    ///   // 场景切换 / 单例销毁时清理：
    ///   DialogEventDispatcher.Shutdown();
    ///
    /// 与 DialogController 的职责划分：
    ///   - 本类只负责「收到请求 → 定位面板 → 显示」；
    ///   - DialogController 仍负责监听 DialogPanel 的 OnConfirm/OnCancel 并发布 DialogResultEvent。
    /// </summary>
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

        /// <summary>
        /// DialogRequestEvent 处理入口 —— 核心定位链路：
        /// GlobalUIMgr → 当前激活 Canvas → GetPanel&lt;DialogPanel&gt; → Show。
        /// 任何一环缺失（GlobalUIMgr 未就绪 / 无激活 Canvas / 面板创建失败）都安全降级，不抛异常。
        /// </summary>
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
                UnityEngine.Debug.LogError("[DialogEventDispatcher] 无法创建 DialogPanel，请确认 Resources/UI/DialogPanel 预制体存在");
                return;
            }

            panel.Show(request);
        }
    }
}
