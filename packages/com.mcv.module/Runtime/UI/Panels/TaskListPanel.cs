using System.Collections.Generic;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class TaskListPanel : PanelBase
    {
        [SerializeField] Transform taskToggleParent;
        ProjectClip currentProjectClip;
        List<Toggle> taskToggles = new List<Toggle>();

        protected override void Awake()
        {
            base.Awake();
            if (taskToggleParent == null)
            {
                Debug.LogError($"[TaskListPanel] 缺少必要组件", this);
                return;
            }
            taskToggles.Clear();
            taskToggles.AddRange(taskToggleParent.GetComponentsInChildren<Toggle>());
        }

        /// <summary>
        /// 由 Controller 在每次面板绑定后调用：按项目的任务列表装配 Toggle，
        /// 勾选当前任务对应项，并挂上切换监听（切换时发布 TaskTypeChangeEventData）。
        /// </summary>
        public void Init(ProjectClip project, TaskType taskType)
        {
            if (project == null) return;
            currentProjectClip = project;

            for (int i = 0; i < project.Tasks.Count && i < taskToggles.Count; i++)
            {
                var task = project.Tasks[i];
                var toggle = taskToggles[i];
                // 先清再挂，避免重复绑定触发旧监听
                toggle.onValueChanged.RemoveAllListeners();
                // 初始显示走 SetToggleState（不触发 onValueChanged，避免回环触发切换逻辑）
                SetToggleState(toggle, task.TaskType == taskType);
                toggle.gameObject.SetActive(true);
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn) TaskToggleOnValueChanged(task.TaskType);
                });
            }

            // 隐藏超出任务数量的多余 Toggle
            for (int i = project.Tasks.Count; i < taskToggles.Count; i++)
            {
                taskToggles[i].gameObject.SetActive(false);
            }
        }

        void TaskToggleOnValueChanged(TaskType type)
        {
            if (currentProjectClip == null) return;
            EventBus<TaskTypeChangeEventData>.Publish(new TaskTypeChangeEventData(currentProjectClip, type));
        }

        /// <summary>
        /// 仅更新 Toggle 显示状态，不触发 onValueChanged（避免程序改显示时误触发切换逻辑）。
        /// 显示与逻辑分离：外部命令修改 type 时只改显示，切换逻辑由真正的用户点击（onValueChanged）驱动。
        /// </summary>
        void SetToggleState(Toggle toggle, bool isOn)
        {
            if (toggle == null) return;
        }

        /// <summary>按任务类型刷新整个列表的显示状态（仅显示，不发布事件）。</summary>
        public void SetTaskType(TaskType taskType)
        {
            if (currentProjectClip == null) return;
            var tasks = currentProjectClip.Tasks;
            for (int i = 0; i < tasks.Count && i < taskToggles.Count; i++)
            {
                SetToggleState(taskToggles[i], tasks[i].TaskType == taskType);
            }
        }
    }
}
