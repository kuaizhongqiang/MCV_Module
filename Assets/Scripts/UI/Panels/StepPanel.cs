using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class StepPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _stepNameText;
        [SerializeField] private TextMeshProUGUI _stepDescriptionText;
        [SerializeField] private TextMeshProUGUI _stepStatusText;

        public void SetStepInfo(string name, string description)
        {
            if (_stepNameText != null) _stepNameText.text = name;
            if (_stepDescriptionText != null) _stepDescriptionText.text = description;
        }

        public void SetStatus(string status)
        {
            if (_stepStatusText != null)
                _stepStatusText.text = status;
        }
    }
}
