using MCV_Module.Data.Project;
using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class TaskEquipmentPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _equipmentNameText;

        public void ShowEquipment(TaskEquipmentData data)
        {
            if (data?.EquipmentData == null) return;
            if (_equipmentNameText != null)
                _equipmentNameText.text = data.EquipmentData.EquipmentName ?? "（仪器待配置）";
        }
    }
}
