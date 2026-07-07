using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class ControlInfoPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _controlText;

        public void SetControlInfo(string text)
        {
            if (_controlText != null)
                _controlText.text = text;
        }
    }
}
