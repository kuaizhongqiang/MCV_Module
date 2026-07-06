using MCV_Module.Data.Project;

namespace MCV_Module.Event
{
    // ────────────────────── 场景加载 ──────────────────────

    public class SceneLoadingEvent
    {
        public string SceneName { get; }
        public float Progress { get; set; }
        public SceneLoadingEvent(string sceneName) { SceneName = sceneName; Progress = 0f; }
    }

    public class SceneLoadedEvent
    {
        public string SceneName { get; }
        public SceneLoadedEvent(string sceneName) { SceneName = sceneName; }
    }

    // ────────────────────── 任务流程 ──────────────────────

    public class TaskChangedEvent
    {
        public TaskType TaskType { get; }
        public TaskChangedEvent(TaskType taskType) { TaskType = taskType; }
    }

    public class TaskCompletedEvent
    {
        public TaskType TaskType { get; }
        public TaskCompletedEvent(TaskType taskType) { TaskType = taskType; }
        // TODO: M4 填入 —— 成绩/完成度数据
    }

    // ────────────────────── 连线交互 ──────────────────────

    public class LineConnectedEvent
    {
        public string GroupId { get; }
        public bool IsConnected { get; }
        public LineConnectedEvent(string groupId, bool isConnected) { GroupId = groupId; IsConnected = isConnected; }
    }

    // ────────────────────── 漫游交互 ──────────────────────

    public class InteractiveClickEvent
    {
        public string ObjectId { get; }
        public InteractiveClickEvent(string objectId) { ObjectId = objectId; }
        // TODO: M3 实现 —— 联动 Panel 信息展示
    }

    // ────────────────────── UI 通用 ──────────────────────

    public class PanelVisibilityEvent
    {
        public string PanelName { get; }
        public bool IsVisible { get; }
        public PanelVisibilityEvent(string panelName, bool isVisible) { PanelName = panelName; IsVisible = isVisible; }
        // TODO: M3 实现 —— 联动其他 Panel 的显隐状态
    }

    // ────────────────────── 步骤系统 (预声明，M3 实现) ──────────────────────

    /// <summary>步骤准备就绪事件 (TODO: M3 实现)</summary>
    public class StepPreparedEvent
    {
        public string StepId { get; }
        public StepPreparedEvent(string stepId) { StepId = stepId; }
    }

    /// <summary>步骤等待交互事件 (TODO: M3 实现)</summary>
    public class StepWaitingEvent
    {
        public string StepId { get; }
        public StepWaitingEvent(string stepId) { StepId = stepId; }
    }

    /// <summary>步骤完成事件 (TODO: M3 实现)</summary>
    public class StepCompletedEvent
    {
        public string StepId { get; }
        public StepCompletedEvent(string stepId) { StepId = stepId; }
    }

    /// <summary>工序切换事件 (TODO: M3 实现)</summary>
    public class ProcessChangedEvent
    {
        public string ProcessingId { get; }
        public ProcessChangedEvent(string processingId) { ProcessingId = processingId; }
    }

    /// <summary>所有步骤完成事件 (TODO: M3 实现)</summary>
    public class AllStepsCompletedEvent { }
}
