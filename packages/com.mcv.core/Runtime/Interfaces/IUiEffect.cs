
namespace MCV_Module.Interfaces
{
    // WHY: UI 特效走 UGUI 事件系统，与 GlobalInteractiveMgr 的 3D 射线互不干扰；组件失活或不可交互时收不到任何事件。
    /// <summary>UI 悬停/点击特效契约：划过、离开、点击三个回调。</summary>
    public interface IUiEffect
    {
        /// <summary>指针进入。</summary>
        void MoEnter();
        /// <summary>指针离开。</summary>
        void MoExit();
        /// <summary>指针点击。</summary>
        void MoClick();
    }
}
