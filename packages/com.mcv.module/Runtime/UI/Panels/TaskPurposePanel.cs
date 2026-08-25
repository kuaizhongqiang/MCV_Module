using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panel
{
    public class TaskPurposePanel : TaskPanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Text contentText;

        public void Init(string title, string content)
        {
            titleText.text = title;
            contentText.text = content;
        }

        public void SetText(string title, string content)
        {
            titleText.text = title;
            contentText.text = content;
        }
    }
}