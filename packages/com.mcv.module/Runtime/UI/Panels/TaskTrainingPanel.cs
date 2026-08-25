using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panel
{
    /// <summary>仿真实验任务面板（占位实现，模仿 TaskPurposePanel）。</summary>
    public class TaskTrainingPanel : TaskPanelBase
    {
        [SerializeField] Text titleText;

        public void Init(string title)
        {
            if (titleText != null) titleText.text = title;
            // TODO: 按 prefabKey 装配仿真实验 UI
        }

        public void SetText(string title)
        {
            Init(title);
        }
    }
}
