using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    /// <summary>仿真实验任务面板（占位实现，模仿 TaskPurposePanel）。</summary>
    public class TaskTrainingPanel : TaskPanelBase
    {
        [SerializeField] Text titleText;
        /// <summary>标题文本节点上的组件（TMP 形态下 Legacy 已被卸载，写入必须经它）。</summary>
        TextComponent m_TitleTextComp;

        protected override void Awake()
        {
            base.Awake();
            // WHY: 必须在卸载之前解析一次 —— Destroy 到帧末才生效，本帧内 titleText 仍可用；之后它就是"假 null"了。
            if (titleText != null) m_TitleTextComp = titleText.GetComponent<TextComponent>();
        }

        public void Init(string title)
        {
            if (m_TitleTextComp != null) m_TitleTextComp.SetText(title);
            else if (titleText != null) TextComponent.SetTextOn(titleText, title);
            // TODO: 按 prefabKey 装配仿真实验 UI
        }

        public void SetText(string title)
        {
            Init(title);
        }

        public override string GetPanelContent()
        {
            // TODO: 获取仿真实验面板内容
            return "";
        }
    }
}
