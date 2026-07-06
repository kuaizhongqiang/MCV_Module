using MCV_Module.Data.Project;

namespace MCV_Module.Event
{
    // ────────────────────── 场景加载 ──────────────────────

    /// <summary>场景加载开始事件</summary>
    public class SceneLoadingEvent
    {
        public string SceneName { get; }
        public float Progress { get; set; }
        public SceneLoadingEvent(string sceneName) { SceneName = sceneName; Progress = 0f; }
    }

    /// <summary>场景加载完成事件</summary>
    public class SceneLoadedEvent
    {
        public string SceneName { get; }
        public SceneLoadedEvent(string sceneName) { SceneName = sceneName; }
    }

    // ────────────────────── 任务流程 ──────────────────────

    /// <summary>任务切换事件</summary>
    public class TaskChangedEvent
    {
        public TaskType TaskType { get; }
        public TaskChangedEvent(TaskType taskType) { TaskType = taskType; }
    }

    /// <summary>任务完成事件</summary>
    public class TaskCompletedEvent
    {
        public TaskType TaskType { get; }
        public TaskCompletedEvent(TaskType taskType) { TaskType = taskType; }
    }

    // ────────────────────── 漫游交互 ──────────────────────

    /// <summary>交互物体点击事件</summary>
    public class InteractiveClickEvent
    {
        public string ObjectId { get; }
        public InteractiveClickEvent(string objectId) { ObjectId = objectId; }
    }

    // ────────────────────── UI 通用 ──────────────────────

    /// <summary>Panel 显示/隐藏事件</summary>
    public class PanelVisibilityEvent
    {
        public string PanelName { get; }
        public bool IsVisible { get; }
        public PanelVisibilityEvent(string panelName, bool isVisible) { PanelName = panelName; IsVisible = isVisible; }
    }
}
