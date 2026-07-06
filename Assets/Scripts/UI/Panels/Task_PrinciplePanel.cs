using MCV_Module.Data.Project;
using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class TaskPrinciplePanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _contentText;

        public void ShowPrinciple(TaskPrincipleData data)
        {
            if (data?.PrincipleData == null) return;
            if (_contentText != null)
                _contentText.text = data.PrincipleData.ShowContent ?? "（实验原理待配置）";
        }
    }
}
