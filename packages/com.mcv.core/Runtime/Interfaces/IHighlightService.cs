using UnityEngine;

namespace MCV_Module.Interfaces
{
    /// <summary>
    /// 高亮服务抽象（core 包零第三方依赖）。
    /// 宿主注入 HighlightPlus 实现（HighlightPlusAdapter）；未注入时高亮功能静默降级（无高亮）。
    /// 行为对齐原 InteractiveBase.HighlightPluginInit/Highlight：Init 只准备效果（不高亮），
    /// ApplyHighlight 惰性初始化后置为高亮，ClearHighlight 仅取消高亮（不销毁效果）。
    /// </summary>
    public interface IHighlightService
    {
        /// <summary>初始化目标对象的高亮效果（添加效果组件、克隆共享 Profile、设置颜色），不触发高亮。</summary>
        void Init(GameObject target, Color color);

        /// <summary>应用高亮（目标未初始化时先初始化）。</summary>
        void ApplyHighlight(GameObject target, Color color);

        /// <summary>取消高亮（目标未初始化时无操作）。</summary>
        void ClearHighlight(GameObject target);

        /// <summary>当前注入的实现；未注入时为 null。</summary>
        static IHighlightService Instance => HighlightServiceRegistry.Instance;

        /// <summary>宿主初始化时调用，注入高亮服务实现。</summary>
        static void Register(IHighlightService impl) => HighlightServiceRegistry.Instance = impl;
    }

    /// <summary>高亮服务静态注册表（配合 IHighlightService 的静态注册使用）。</summary>
    public static class HighlightServiceRegistry
    {
        public static IHighlightService Instance { get; set; }
    }
}
