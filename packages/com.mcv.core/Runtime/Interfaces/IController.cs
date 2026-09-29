using MCV_Module.UI;

namespace MCV_Module.Interfaces
{
    // WHY: 控制器与面板按 1:1 名字约定配对（TitleController ↔ TitlePanel）；名字对不上，面板生命周期就找不到控制器、Bind 永不触发。
    // WHY: 控制器不再是 MonoBehaviour（不挂场景），其生命周期由 GlobalControllerMgr 显式驱动：注册后 OnInit，销毁前 OnDispose。
    /// <summary>面板控制器契约：按名字绑定对应 View，生命周期由 GlobalControllerMgr 显式驱动。</summary>
    public interface IController
    {
        /// <summary>控制器名（须与对应 View/Panel 名字一致）。</summary>
        string ControllerName { get; }

        /// <summary>注册完成后回调（由 GlobalControllerMgr 在创建并登记后调用一次）。</summary>
        void OnInit();

        /// <summary>销毁前回调（由 GlobalControllerMgr 在停止协程后调用一次）。</summary>
        void OnDispose();

        /// <summary>由面板生命周期调用（1:1 名字约定），绑定对应的 View。</summary>
        void Bind(PanelBase panel);
    }
}
