using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MCV_Module.Models.Project;

namespace MCV_Module.UI.Panel
{
    /// <summary>实验仪器任务面板（占位实现，模仿 TaskPurposePanel）。</summary>
    public class TaskEquipmentPanel : TaskPanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Transform equipmentParent;

        readonly List<EquipmentStruct> equipmentList = new List<EquipmentStruct>();

        public void Init(List<EquipmentStruct> equipmentStructs)
        {
            if (equipmentStructs == null) return;
            equipmentList.Clear();
            equipmentList.AddRange(equipmentStructs);
            // TODO: 按 equipmentList 装配 UI（每个 EquipmentStruct: prefabKey/title/contentText/audioName）
        }

        public void SetEquipment(List<EquipmentStruct> equipmentStructs)
        {
            Init(equipmentStructs);
        }
    }
}
