using MCV_Module.Data.Project;
using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class TaskLineConnectionPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _statusText;

        public void ShowLineConnection(TaskLineConnectionData data)
        {
            // TODO: M2e 填入完整连线交互 UI
            if (_statusText != null)
                _statusText.text = "连线模式（待 M2e 实现交互）";
        }
    }
}
