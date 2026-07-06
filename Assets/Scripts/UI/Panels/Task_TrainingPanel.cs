using MCV_Module.Data.Project;
using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class TaskTrainingPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _tipText;

        public void ShowTraining(TaskTrainingData data)
        {
            if (data?.TipsData == null) return;
            if (_tipText != null)
                _tipText.text = data.TipsData.TipText ?? "（实训待配置）";
        }
    }
}
