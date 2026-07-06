using MCV_Module.Data.Project;
using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class TaskPurposePanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _contentText;

        public void ShowPurpose(TaskPurposeData data)
        {
            if (data?.SpeakingData == null) return;
            if (_contentText != null)
                _contentText.text = data.SpeakingData.ShowContent ?? "（实验目的待配置）";
        }
    }
}
