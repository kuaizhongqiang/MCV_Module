using System;

namespace MCV_Module.UI
{
    // WHY: 未标注的（历史）面板由 PanelBase 回退到 XxxPanel → XxxController 约定；本特性由 MCV Editor/创建/UI Panel 生成器自动写入
    /// <summary>面板 ↔ Controller 强绑定特性：编译期指定 Controller 类型，替代字符串命名约定。</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class RequireControllerAttribute : Attribute
    {
        /// <summary>绑定的 Controller 类型（实现 IController，注册名 = 类型名）。</summary>
        public Type ControllerType { get; }

        public RequireControllerAttribute(Type controllerType)
        {
            ControllerType = controllerType;
        }
    }
}
