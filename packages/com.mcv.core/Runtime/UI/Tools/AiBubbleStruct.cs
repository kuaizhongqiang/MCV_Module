using UnityEngine;
using MCV_Module.Utils;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Tools
{
    // WHY: 预制体加载失败只降级为 bubble = null（不抛异常），CreateBubble 又在构造函数里就跑，所以所有使用点都必须判空。
    /// <summary>AI 对话气泡基类：封装预制体加载、实例化、文本写入与布局刷新。</summary>
    public abstract class AiBubbleStructBase
    {
        protected Transform parent;
        protected Text content;                      // 暂时为Text 之后会更换TextComponent组件
        // WHY: 正文节点上的组件必须一起持有 —— TMP 形态下组件会卸载节点上的 Legacy Text，content 随即成"假 null"，
        // 静态入口 SetTextOn / ReadRaw 会静默失效（AI 正文一个字都写不进去）。
        /// <summary>正文节点上的组件（解析点见 <see cref="CreateBubble"/>）。</summary>
        protected TextComponent contentComp;
        protected GameObject bubble;

        // WHY: 三者与预制体硬绑定（UI 包预制体名（取件见 UIPrefabUtil）与子物体层级索引），写错不报错、只显示错位，改预制体必须同步这里。
        protected abstract string PrefabName { get; }
        protected abstract string BubbleName { get; }
        protected abstract int ContentChildIndex { get; }

        protected AiBubbleStructBase(Transform parent)
        {
            this.parent = parent;
            bubble = CreateBubble();
        }

        protected GameObject CreateBubble()
        {
            GameObject prefab = UIPrefabUtil.Get(PrefabName);
            if (prefab == null)
            {
                Log.Warning($"[AiBubble] 缺少气泡预制体: {PrefabName}, 气泡 {BubbleName} 无法显示");
                return null;
            }
            GameObject go = GameObject.Instantiate(prefab, parent);
            go.name = BubbleName;

            content = GetText(go.transform);
            // WHY: 组件与 Text 必须在**同一处**解析 —— 这里是"首次拿到 Text"的那一刻，实例刚 Instantiate（Legacy Text 还在）；
            // 等 TMP 形态把它卸载后再懒解析，字段已是"假 null"、对它取 GetComponent 会抛。
            contentComp = GetTextComponent(go.transform);
            // 纯文本渲染: 关闭 RichText 解析, 确保任何 <xxx> 标签按普通文本原样显示(不做任何格式转换)
            if (content != null)
            {
                content.supportRichText = false;
            }
            return go;
        }

        protected virtual Text GetText(Transform parent)
        {
            return parent.GetChild(0).GetChild(ContentChildIndex).GetComponent<Text>();
        }

        // WHY: 走 GetText 的同一条层级路径取**同一节点**上的组件 —— TMP 形态下这一层只剩组件（Legacy 被卸载），
        // 所以判空与写入都必须以组件为准，只在节点上压根没有组件时才退回 Text。
        /// <summary>从正文节点上取文本组件（层级路径与 <see cref="GetText"/> 一致）。</summary>
        protected virtual TextComponent GetTextComponent(Transform parent)
        {
            return parent.GetChild(0).GetChild(ContentChildIndex).GetComponent<TextComponent>();
        }

        // WHY: 正文必须保持纯文本（supportRichText = false），一旦开启富文本解析，AI 输出里的 <xxx> 会被当标签吞掉。
        /// <summary>设置气泡正文文本：纯文本，不做任何 markdown/RichText 转换。</summary>
        public void SetText(string text)
        {
            // WHY: 判空连组件一起看 —— TMP 形态下 content 是"假 null"但组件在，只看字段会让气泡永远写不进正文。
            if (content == null && contentComp == null) return;
            if (bubble == null)
            {
                bubble = CreateBubble();
                if (bubble == null) return;   // 预制体缺失
                // WHY: 懒创建这条路径也要把组件一并补上（CreateBubble 的前半段可能已因预制体缺失返回过一次，只补 content 会留下半截缓存）。
                if (content == null)
                {
                    content = GetText(bubble.transform);
                }
                if (contentComp == null)
                {
                    contentComp = GetTextComponent(bubble.transform);
                }
            }
            if (ReadContent() == text) return;
            if (contentComp != null) contentComp.SetText(text);
            else TextComponent.SetTextOn(content, text);
            RebuildLayout(contentComp != null ? contentComp.transform : null);
        }

        // WHY: 读回必须优先走组件 —— 换过形态后静态入口 ReadRaw(content) 只返回空串，"内容相同就跳过"会永远不成立（流式逐段调用每次整段重写）。
        /// <summary>读回正文当前原文（有组件走组件）。</summary>
        protected string ReadContent()
        {
            if (contentComp != null) return contentComp.RawText;
            return content != null ? TextComponent.ReadRaw(content) : string.Empty;
        }

        /// <summary>设置气泡正文(纯文本)。与 SetText 等价, 保留为流式逐段调用的清晰入口。</summary>
        public void SetTextPlain(string text)
        {
            SetText(text);
        }

        // WHY: 布局必须自下而上（子先父后）—— 只刷最外层会让父级 LayoutGroup 先量到子级的旧尺寸（气泡框宽高慢一拍）。
        // WHY: 等一帧那一层交给宿主面板起协程（面板走 PanelBase.RequestLayoutRebuild）；普通类不持有协程（见 Tools/README
        //      的约定），故这里保持同步、由 UILayoutRebuilder 保证刷的顺序，气泡高度由流式后续分片自然收敛。
        /// <summary>按锚点自下而上刷一次布局；anchor 传刚改动的那个文本节点，为空时退到气泡根。</summary>
        protected void RebuildLayout(Transform anchor = null)
        {
            if (anchor == null)
            {
                if (contentComp != null) anchor = contentComp.transform;
                else if (content != null) anchor = content.transform;
                else if (bubble != null) anchor = bubble.transform;
            }
            if (anchor == null || parent == null) return;

            // 刷到容器之上那一层（与旧实现的落点一致，但改成从文本节点逐级向上）；容器就是根节点时止于容器本身，别一路刷到场景根
            UILayoutRebuilder.RebuildChain(anchor, parent.parent != null ? parent.parent : parent);
        }
    }

    /// <summary>AI 系统对话气泡（UI 包预制体 Ai_System_Bubble）。</summary>
    public class AiSystemBubbleStruct : AiBubbleStructBase
    {
        protected override string PrefabName => "Ai_System_Bubble";
        protected override string BubbleName => "SystemBubble";
        protected override int ContentChildIndex => 1;

        public AiSystemBubbleStruct(Transform parent) : base(parent)
        {
        }
    }

    /// <summary>AI 用户对话气泡（UI 包预制体 Ai_User_Bubble）。</summary>
    public class AiUserBubbleStruct : AiBubbleStructBase
    {
        protected override string PrefabName => "Ai_User_Bubble";
        protected override string BubbleName => "UserBubble";
        protected override int ContentChildIndex => 1;

        public AiUserBubbleStruct(Transform parent) : base(parent)
        {
        }
    }

    // WHY: 预制体层级是硬约定：根 → Child0 → Child1 思考气泡 / Child2 正文，思考气泡内部 → Child0 → Child1 思考文本，改层级会取到错组件。
    /// <summary>AI 助手对话气泡：正文之外多一个可单独显隐、单独写入的思考区。</summary>
    public class AiAssistantBubbleStruct : AiBubbleStructBase
    {
        protected Text reasoningContent;
        // WHY: 与正文同理 —— TMP 形态下节点上的 Legacy Text 会被卸载，reasoningContent 成"假 null"，
        // 必须同时持有思考区节点上的组件，否则思考文本静默写不进去（表现为"思考区一直空着"）。
        /// <summary>思考区节点上的组件（解析点见构造函数与 <see cref="SetReasoningText"/>）。</summary>
        protected TextComponent reasoningContentComp;
        protected GameObject reasoningBubble;

        protected override string PrefabName => "Ai_Assistant_Bubble";
        protected override string BubbleName => "AssistantBubble";
        protected override int ContentChildIndex => 2;

        public AiAssistantBubbleStruct(Transform parent) : base(parent)
        {
            // 构造时即缓存正文与思考引用(从气泡自身 transform 查找, 而非面板 parent)
            if (bubble != null)
            {
                reasoningBubble = GetReasoningBubble(bubble.transform);
                reasoningContent = GetReasoningText(bubble.transform);
                // WHY: 与 reasoningContent 在**同一处**解析 —— 此刻气泡刚 Instantiate、Legacy Text 还在；事后懒解析会碰上"假 null"。
                reasoningContentComp = GetReasoningTextComponent(bubble.transform);

                // WHY: 思考区默认收起 —— 预制体里 Reasoning_Bubble 是**激活**的，只有"真有思考内容"时才由
                //      SetReasoningBubbleActive(true) 展开。不在构造处兜底的话，凡是不经 BeginAssistantReply 的创建路径
                //      （AddAssistantMessage / SetAssistantText / SetAssistantReasoningText…）都会留下一个空的思考气泡。
                if (reasoningBubble != null) reasoningBubble.SetActive(false);
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

        // WHY: 与 GetReasoningText 走同一条层级路径取**同一节点**上的组件（TMP 形态下这一层只剩组件），判空与写入以它为准。
        /// <summary>从思考区节点上取文本组件（层级路径与 <see cref="GetReasoningText"/> 一致）。</summary>
        protected TextComponent GetReasoningTextComponent(Transform root)
        {
            if (reasoningBubble == null) reasoningBubble = GetReasoningBubble(root);
            if (reasoningBubble == null) return null;
            return reasoningBubble.transform.GetChild(0).GetChild(1).GetComponent<TextComponent>();
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

        /// <summary>设置思考区文本：纯文本，不做任何 markdown/RichText 转换。</summary>
        public void SetReasoningText(string text)
        {
            if ((reasoningContent == null || reasoningContentComp == null) && bubble != null)
            {
                reasoningBubble = GetReasoningBubble(bubble.transform);
                reasoningContent = GetReasoningText(bubble.transform);
                // WHY: 这里同样是"首次拿到 Text"的懒解析路径，组件必须一起补上，否则 TMP 形态下后续思考文本全部静默写不进去。
                reasoningContentComp = GetReasoningTextComponent(bubble.transform);
            }
            if (reasoningContent == null && reasoningContentComp == null) return;
            if (ReadReasoningContent() == text) return;
            if (reasoningContentComp != null) reasoningContentComp.SetText(text);
            else TextComponent.SetTextOn(reasoningContent, text);
            RebuildLayout(reasoningContentComp != null ? reasoningContentComp.transform : null);
        }

        // WHY: 换过形态后静态入口读回空串，"内容相同就跳过"会永远不成立（流式逐段调用会每次整段重写、布局反复重建）。
        /// <summary>读回思考区当前原文（有组件走组件）。</summary>
        string ReadReasoningContent()
        {
            if (reasoningContentComp != null) return reasoningContentComp.RawText;
            return reasoningContent != null ? TextComponent.ReadRaw(reasoningContent) : string.Empty;
        }

        /// <summary>设置思考文本(纯文本)。与 SetReasoningText 等价, 保留为流式逐段调用的清晰入口。</summary>
        public void SetReasoningTextPlain(string text)
        {
            SetReasoningText(text);
        }
    }
}
