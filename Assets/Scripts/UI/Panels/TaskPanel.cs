using System.Collections.Generic;
using MCV_Module.Data.Project;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class TaskPanel : PanelBase
    {
        [SerializeField] private Transform _taskItemContainer;
        [SerializeField] private GameObject _taskItemPrefab;

        private readonly List<GameObject> _taskItems = new List<GameObject>();

        public void BuildTaskList(List<TaskDataBase> tasks, System.Action<TaskType> onClick)
        {
            ClearItems();

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task == null) continue;

                var item = Instantiate(_taskItemPrefab, _taskItemContainer);
                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = task.displayName;

                var taskType = task.TaskType;
                var btn = item.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => onClick?.Invoke(taskType));

                _taskItems.Add(item);
            }
        }

        public void UpdateTaskStatus(TaskType activeType)
        {
            // TODO: 切换高亮当前任务项样式
        }

        private void ClearItems()
        {
            foreach (var item in _taskItems)
                Destroy(item);
            _taskItems.Clear();
        }
    }
}
