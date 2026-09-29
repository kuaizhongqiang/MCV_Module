using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    public class TaskPurposePanel : TaskPanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Text contentText;

        TextComponent m_TitleTextComp;   // 见 Awake 的 WHY：换形态后 titleText 会变成假 null，写入必须经组件
        TextComponent m_ContentTextComp; // 同上（contentText）

        protected override void Awake()
        {
            base.Awake();
            // WHY: 必须在卸载之前解析并缓存 —— TMP 形态下本组件会卸载节点上的 Legacy Text，
            // 而 Destroy 到帧末才生效，故本帧内 GetComponent 仍稳定可用；之后这两个字段都成假 null。
            if (titleText != null) m_TitleTextComp = titleText.GetComponent<TextComponent>();
            if (contentText != null) m_ContentTextComp = contentText.GetComponent<TextComponent>();
        }

        public void Init(string title, string content)
        {
            if (m_TitleTextComp != null) m_TitleTextComp.SetText(title);
            else TextComponent.SetTextOn(titleText, title);
            if (m_ContentTextComp != null) m_ContentTextComp.SetText(content);
            else TextComponent.SetTextOn(contentText, content);
        }

        public void SetText(string title, string content)
        {
            if (m_TitleTextComp != null) m_TitleTextComp.SetText(title);
            else TextComponent.SetTextOn(titleText, title);
            if (m_ContentTextComp != null) m_ContentTextComp.SetText(content);
            else TextComponent.SetTextOn(contentText, content);
        }

        public override string GetPanelContent()
        {
            string result = "";
            result += "【任务目的页面】\n";
            result += $"当前显示内容为{(m_TitleTextComp != null ? m_TitleTextComp.RawText : TextComponent.ReadRaw(titleText))},{(m_ContentTextComp != null ? m_ContentTextComp.RawText : TextComponent.ReadRaw(contentText))}\n";
            return result;
        }
    }
}