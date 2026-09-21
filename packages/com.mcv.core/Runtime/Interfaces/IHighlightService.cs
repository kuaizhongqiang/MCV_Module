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
        /// <summary>
        /// 初始化目标对象的高亮效果（挂效果组件、应用共享 Profile / 外观配置），不触发高亮。
        /// color 由调用方按业务传入（如 InteractiveBase.highlightColor）；实现可忽略它、改用统一的 Profile 决定外观。
        /// </summary>
        void Init(GameObject target, Color color);

        /// <summary>应用高亮（目标未初始化时先初始化）。</summary>
        void ApplyHighlight(GameObject target, Color color);

        /// <summary>取消高亮（目标未初始化时无操作）。</summary>
        void ClearHighlight(GameObject target);

        /// <summary>
        /// 目标的高亮是否"穿透遮挡绘制"（HighlightPlus 对应 Outer Glow 的 Visibility = AlwaysOnTop）。
        /// 用于高亮物常被其它几何体遮住、必须始终可见的场景。
        /// 只影响该目标的可见性绘制，不改 Profile 里的其它外观；未注入实现时静默降级。
        /// 注意：外观以 Profile 为准，实现方需在每次应用高亮时重申该偏好，避免被 Profile 回灌覆盖。
        /// </summary>
        void SetHighlightOnTop(GameObject target, bool onTop);

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
