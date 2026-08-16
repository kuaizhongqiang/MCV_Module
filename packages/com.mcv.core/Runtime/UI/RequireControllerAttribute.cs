using System;

namespace MCV_Module.UI
{
    /// <summary>
    /// 面板 ↔ Controller 强绑定特性：编译期指定对应 Controller 类型，
    /// 替代纯字符串命名约定（XxxPanel → XxxController）。
    /// 未标注时 PanelBase 回退字符串约定（兼容历史面板）。
    /// 由 MCV/创建/UI Panel 生成器自动写入。
    /// </summary>
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
