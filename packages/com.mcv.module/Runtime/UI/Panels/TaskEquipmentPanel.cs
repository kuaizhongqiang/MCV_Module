using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MCV_Module.Models.Project;
using MCV_Module.Managers;
using MCV_Module.Event;
using MCV_Module.Models;

namespace MCV_Module.UI.Panel
{
    /// <summary>实验仪器任务面板（占位实现，模仿 TaskPurposePanel）。</summary>
    public class TaskEquipmentPanel : TaskPanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Text contentText;
        [SerializeField] Transform equipmentParent;
        EquipmentStruct currentEquipment;
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

        public void SelectEquipment(string prefabKey)
        {
            var equipment = equipmentList.Find(x => x.prefabKey == prefabKey);
            currentEquipment = equipment;
            titleText.text = currentEquipment.title;
            contentText.text = currentEquipment.contentText;
            var audioEvent = new AudioPlayEventData(currentEquipment.audioName,AudioSouceType.Speaker);
            EventBus<AudioPlayEventData>.Publish(audioEvent);
        }

        public override string GetPanelContent()
        {
            string result = "";
            int equipmentCount = equipmentList.Count;
            result += "【实验仪器页面】\n";
            result += $"当前任务包含 {equipmentCount} 个实验仪器。\n";
            result += $"分别是：\n";
            for (int i = 0; i < equipmentCount; i++)
            {
                var item = equipmentList[i];
                result += $"{item.title}\n";
            }
            result += $"当前显示的仪器是：\n";
            result += $"{currentEquipment.title}\n";
            result += $"{currentEquipment.contentText}\n";

            return result;
        }
    }
}
