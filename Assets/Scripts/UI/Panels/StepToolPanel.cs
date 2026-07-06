using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class StepToolPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _toolNameText;
        [SerializeField] private TextMeshProUGUI _toolDescriptionText;

        public void ShowTool(string name, string description)
        {
            if (_toolNameText != null) _toolNameText.text = name;
            if (_toolDescriptionText != null) _toolDescriptionText.text = description;
            SetUIActive(true);
        }

        public void HideTool()
        {
            SetUIActive(false);
        }
    }
}
