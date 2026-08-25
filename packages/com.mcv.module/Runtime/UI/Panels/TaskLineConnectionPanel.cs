using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panel
{
    /// <summary>电路连接任务面板（占位实现，模仿 TaskPurposePanel）。</summary>
    public class TaskLineConnectionPanel : TaskPanelBase
    {
        [SerializeField] Text titleText;

        public void Init(string title)
        {
            if (titleText != null) titleText.text = title;
            // TODO: 按 prefabKey 装配电路连接 UI
        }

        public void SetText(string title)
        {
            Init(title);
        }
    }
}
