using MCV_Module.UI;

namespace MCV_Module.Interfaces
{
    public interface IController
    {
        string ControllerName { get; }
        void OnBindView();

        /// <summary>
        /// 由面板生命周期调用（1:1 名字约定），绑定对应的 View。
        /// </summary>
        void Bind(PanelBase panel);
    }
}