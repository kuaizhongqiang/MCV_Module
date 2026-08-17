using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Tools
{
    /// <summary>
    /// AI对话气泡基类：封装气泡预制体加载、实例化、文本设置与布局刷新等公共逻辑。
    /// </summary>
    public abstract class AiBubbleStructBase
    {
        protected Transform parent;
        protected Text content;                      // 暂时为Text 之后会更换TextComponent组件
        protected GameObject bubble;

        protected abstract string PrefabPath { get; }
        protected abstract string BubbleName { get; }
        protected abstract int ContentChildIndex { get; }

        protected AiBubbleStructBase(Transform parent)
        {
            this.parent = parent;
            bubble = CreateBubble();
        }

        protected GameObject CreateBubble()
        {
            GameObject prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[AiBubble] 缺少气泡预制体: Resources/{PrefabPath}, 气泡 {BubbleName} 无法显示");
                return null;
            }
            GameObject go = GameObject.Instantiate(prefab, parent);
            go.name = BubbleName;

            content = GetText(go.transform);
            return go;
        }

        protected virtual Text GetText(Transform parent)
        {
            return parent.GetChild(0).GetChild(ContentChildIndex).GetComponent<Text>();
        }

        /// <summary>
        /// 设置气泡正文文本。
        /// </summary>
        public void SetText(string text)
        {
            if (content == null) return;
            if (content.text == text) return;
            if (bubble == null)
            {
                bubble = CreateBubble();
                if (bubble == null) return;   // 预制体缺失
                if (content == null)
                {
                    content = GetText(bubble.transform);
                }
            }

            content.text = text;
            RebuildLayout();
        }

        protected void RebuildLayout()
        {
            RectTransform rect = parent.parent.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }

    /// <summary>
    /// AI系统对话气泡
    /// </summary>
    public class AiSystemBubbleStruct : AiBubbleStructBase
    {
        protected override string PrefabPath => "UI/Ai_System_Bubble";
        protected override string BubbleName => "SystemBubble";
        protected override int ContentChildIndex => 1;

        public AiSystemBubbleStruct(Transform parent) : base(parent)
        {
        }
    }

    /// <summary>
    /// AI用户对话气泡
    /// </summary>
    public class AiUserBubbleStruct : AiBubbleStructBase
    {
        protected override string PrefabPath => "UI/Ai_User_Bubble";
        protected override string BubbleName => "UserBubble";
        protected override int ContentChildIndex => 1;

        public AiUserBubbleStruct(Transform parent) : base(parent)
        {
        }
    }

    /// <summary>
    /// AI助手对话气泡(含思考区 + 正文区)。
    /// 预制体结构约定: 根节点 → Child0(内容根) → Child1=思考气泡, Child2=正文Text;
    /// 思考气泡内部 → Child0 → Child1=思考Text。
    /// </summary>
    public class AiAssistantBubbleStruct : AiBubbleStructBase
    {
        protected Text reasoningContent;
        protected GameObject reasoningBubble;

        protected override string PrefabPath => "UI/Ai_Assistant_Bubble";
        protected override string BubbleName => "AssistantBubble";
        protected override int ContentChildIndex => 2;

        public AiAssistantBubbleStruct(Transform parent) : base(parent)
        {
            // 构造时即缓存正文与思考引用(从气泡自身 transform 查找, 而非面板 parent)
            if (bubble != null)
            {
                reasoningBubble = GetReasoningBubble(bubble.transform);
                reasoningContent = GetReasoningText(bubble.transform);
            }
        }

        protected override Text GetText(Transform parent)
        {
            return parent.GetChild(0).GetChild(2).GetComponent<Text>();
        }

        protected Text GetReasoningText(Transform root)
        {
            if (reasoningBubble == null) reasoningBubble = GetReasoningBubble(root);
            return reasoningBubble.transform.GetChild(0).GetChild(1).GetComponent<Text>();
        }

        protected GameObject GetReasoningBubble(Transform root)
        {
            return root.GetChild(0).GetChild(1).gameObject;
        }

        public void SetReasoningBubbleActive(bool active)
        {
            if (reasoningBubble == null && bubble != null)
                reasoningBubble = GetReasoningBubble(bubble.transform);
            if (reasoningBubble != null)
                reasoningBubble.SetActive(active);
        }

        public void SetContentText(string text)
        {
            SetText(text);
        }

        public void SetReasoningText(string text)
        {
            if (reasoningContent == null && bubble != null)
            {
                reasoningBubble = GetReasoningBubble(bubble.transform);
                reasoningContent = GetReasoningText(bubble.transform);
            }
            if (reasoningContent == null) return;
            if (reasoningContent.text == text) return;
            reasoningContent.text = text;
            RebuildLayout();
        }
    }
}
