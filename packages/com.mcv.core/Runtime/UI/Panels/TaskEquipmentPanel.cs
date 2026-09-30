using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MCV_Module.Models.Project;
using MCV_Module.Managers;
using MCV_Module.Event;
using MCV_Module.UI.Components;
using MCV_Module.Utils;
using MCV_Module.Models;
using MCV_Module.Utils;


namespace MCV_Module.UI.Panels
{
    /// <summary>实验仪器任务面板（占位实现，模仿 TaskPurposePanel）。</summary>
    public class TaskEquipmentPanel : TaskPanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Text contentText;
        [SerializeField] Transform equipmentParent;

        // WHY: 两个文本节点上的组件要提前缓存 —— TMP 形态下组件会卸载节点上的 Legacy Text，字段随后成"假 null"，
        // 静态入口 SetTextOn 会静默 no-op（选中仪器后标题与说明不更新）。
        /// <summary>标题节点上的组件（缓存见上）。</summary>
        TextComponent m_TitleTextComp;
        /// <summary>说明文本节点上的组件（缓存见上）。</summary>
        TextComponent m_ContentTextComp;

        EquipmentStruct currentEquipment;
        readonly List<EquipmentStruct> equipmentList = new List<EquipmentStruct>();

        protected override void Awake()
        {
            base.Awake();

            // WHY: 尽早解析一次（此时节点上的 Legacy Text 还在，GetComponent 稳定可用）；换形态后再解析会抛。
            if (titleText != null) m_TitleTextComp = titleText.GetComponent<TextComponent>();
            if (contentText != null) m_ContentTextComp = contentText.GetComponent<TextComponent>();
        }

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
            string title = Localized.Pick(currentEquipment.title, currentEquipment.titleEn);
            string content = Localized.Pick(currentEquipment.contentText, currentEquipment.contentTextEn);
            if (m_TitleTextComp != null) m_TitleTextComp.SetText(title);
            else TextComponent.SetTextOn(titleText, title);
            if (m_ContentTextComp != null) m_ContentTextComp.SetText(content);
            else TextComponent.SetTextOn(contentText, content);
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
