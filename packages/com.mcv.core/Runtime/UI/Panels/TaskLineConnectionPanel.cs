using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    /// <summary>电路连接任务面板（占位实现，模仿 TaskPurposePanel）。</summary>
    public class TaskLineConnectionPanel : TaskPanelBase
    {
        [SerializeField] Text titleText;

        /// <summary>标题节点上的组件（TMP 形态下节点上的 Legacy Text 被卸载，字段随后成"假 null"，静态入口静默 no-op）。</summary>
        TextComponent m_TitleTextComp;

        protected override void Awake()
        {
            base.Awake();

            // WHY: 尽早解析一次（此时节点上的 Legacy Text 还在，GetComponent 稳定可用）；换形态后再解析会抛。
            if (titleText != null) m_TitleTextComp = titleText.GetComponent<TextComponent>();
        }

        public void Init(string title)
        {
            if (m_TitleTextComp != null) m_TitleTextComp.SetText(title);
            else TextComponent.SetTextOn(titleText, title);
            // TODO: 按 prefabKey 装配电路连接 UI
        }

        public void SetText(string title)
        {
            Init(title);
        }

        public override string GetPanelContent()
        {
            // TODO: 获取电路连接面板内容
            return null;
        }
    }
}
