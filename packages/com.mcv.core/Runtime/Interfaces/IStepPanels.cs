using System;

namespace MCV_Module.Interfaces
{
    // WHY: 条件按名字经 GlobalControllerMgr 查找面板控制器、依赖接口而非具体类；当前未实现时打告警并跳过，绝不阻塞步骤流程。
    /// <summary>步骤功能面板最小契约：Tool/UI/Question 三类面板均由条件按名字查找。</summary>

    /// <summary>工具面板契约：注册工具项 + 用户按下工具起拖 + 拖拽光标状态</summary>
    public interface IStepToolPanel : IController
    {
        /// <summary>用户按下工具项（参数为 toolId）；条件仅在 pressedId == 目标 usingId 时响应</summary>
        event Action<string> OnToolPressed;
        /// <summary>打开工具面板</summary>
        void ShowPanel();
        /// <summary>设置/清除拖拽中的工具（隐藏被拖工具 ICO；null 表示结束拖拽）</summary>
        void SetToolDragging(string toolId);
        /// <summary>关闭工具面板（跳转回来/步骤完成时兜底）</summary>
        void ClosePanel();
    }

    /// <summary>信息面板契约：按 uiId 显示内容，用户关闭面板即完成</summary>
    public interface IStepUiPanel : IController
    {
        /// <summary>用户关闭信息面板</summary>
        event Action OnPanelClosed;
        /// <summary>按 uiId 显示数据（usingId 即 uiId）</summary>
        void ShowData(string uiId);
        /// <summary>关闭信息面板</summary>
        void ClosePanel();
    }

    /// <summary>答题面板契约：按 questionId 出题，答对后触发 OnQuestionCorrect</summary>
    public interface IStepQuestionPanel : IController
    {
        /// <summary>用户答对题目</summary>
        event Action OnQuestionCorrect;
        /// <summary>按 questionId 显示题目（usingId 即 questionId）</summary>
        void ShowQuestion(string questionId);
        /// <summary>关闭答题面板</summary>
        void ClosePanel();
    }
}
