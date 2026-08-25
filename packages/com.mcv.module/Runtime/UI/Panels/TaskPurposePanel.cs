using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
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

        public override string GetPanelContent()
        {
            string result = "";
            result += "【任务目的页面】\n";
            result += $"当前显示内容为{titleText.text},{contentText.text}\n";
            return result;
        }
    }
}