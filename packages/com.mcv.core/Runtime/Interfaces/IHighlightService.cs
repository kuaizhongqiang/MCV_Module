using UnityEngine;

namespace MCV_Module.Interfaces
{
    // WHY: 未注入实现时必须静默降级为「无高亮」，禁止报错——框架包零第三方依赖，且不得出现任何插件类型。
    /// <summary>高亮服务抽象：宿主注入实现，未注入时高亮静默降级。</summary>
    public interface IHighlightService
    {
        // WHY: color 只是提示，实现方可忽略（外观由实现方自行决定）；实现方若有共享外观资产，不得把 color 写进去覆盖它。
        /// <summary>准备目标的高亮效果（挂组件 ＋ 应用外观），不触发高亮。</summary>
        void Init(GameObject target, Color color);

        /// <summary>应用高亮（目标未初始化时先初始化）。</summary>
        void ApplyHighlight(GameObject target, Color color);

        /// <summary>取消高亮（目标未初始化时无操作）。</summary>
        void ClearHighlight(GameObject target);

        // WHY: 实现方若用「共享外观资产」驱动外观，回灌时会覆盖可见性，故必须在每次应用高亮时重申该偏好，否则穿透遮挡会失效。
        /// <summary>目标高亮是否"穿透遮挡绘制"（只改可见性、不动其它外观）。</summary>
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
