using System;
using System.Collections.Generic;
using System.Text;
using MCV_Module.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>
    /// AI 对话面板 —— 只负责展示与输入, 不碰 AI 逻辑。
    ///
    /// 数据流:
    ///   用户在输入框输入 -> SubmitInput() -> OnSendRequested 事件 -> Controller 处理
    ///   Controller 回填: AddUserMessage / BeginAssistantReply / AppendAssistantContent / AppendAssistantReasoning
    /// </summary>
    public class AiDialogPanel : PanelBase
    {
        readonly List<AiBubbleStructBase> bubbleList = new List<AiBubbleStructBase>();
        readonly StringBuilder assistantContent = new StringBuilder();
        readonly StringBuilder assistantReasoning = new StringBuilder();

        [SerializeField] Transform bubbleParent;
        [SerializeField] InputField inputField;
        [SerializeField] Button summitBtn;
        [SerializeField] Text infoText;

        AiBubbleStructBase currentBubble;
        bool hasReasoning;

        /// <summary>用户提交输入时触发(携带文本), 由 Controller 订阅。先清后加, 避免重复订阅。</summary>
        public event Action<string> OnSendRequested;

        /// <summary>是否已有消息(用于首次展示欢迎语)</summary>
        public bool HasMessage { get { return bubbleList.Count > 0; } }

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            if (bubbleParent == null)
            {
                Debug.LogError("AiDialogPanel: 需要手动挂载 bubbleParent！");
                return;
            }

            ClearChildren(bubbleParent);

            if (summitBtn != null)
                summitBtn.onClick.AddListener(SubmitInput);
            if (inputField != null)
                inputField.onSubmit.AddListener(OnInputSubmit);
        }

        protected override void OnDestroy()
        {
            if (summitBtn != null)
                summitBtn.onClick.RemoveListener(SubmitInput);
            if (inputField != null)
                inputField.onSubmit.RemoveListener(OnInputSubmit);
            base.OnDestroy();
        }
        #endregion

        #region 消息展示
        /// <summary>添加一条系统消息(欢迎语/提示)</summary>
        public void AddSystemMessage(string text)
        {
            CreateSystemBubble();
            SetSystemText(text);
        }

        /// <summary>添加一条用户消息</summary>
        public void AddUserMessage(string text)
        {
            CreateUserBubble();
            SetUserText(text);
            ScrollToBottom();
        }

        /// <summary>添加一条完整助手消息(非流式场景)</summary>
        public void AddAssistantMessage(string text)
        {
            CreateAssistantBubble();
            SetAssistantText(text);
            ScrollToBottom();
        }

        /// <summary>清空所有气泡</summary>
        public void ClearAll()
        {
            ClearChildren(bubbleParent);
            bubbleList.Clear();
            currentBubble = null;
            assistantContent.Length = 0;
            assistantReasoning.Length = 0;
            hasReasoning = false;
        }

        /// <summary>创建系统气泡(供流式/分段填充前先建壳)</summary>
        public void CreateSystemBubble()
        {
            currentBubble = CreateBubble<AiSystemBubbleStruct>();
        }

        /// <summary>创建用户气泡</summary>
        public void CreateUserBubble()
        {
            currentBubble = CreateBubble<AiUserBubbleStruct>();
        }

        /// <summary>创建助手气泡</summary>
        public void CreateAssistantBubble()
        {
            currentBubble = CreateBubble<AiAssistantBubbleStruct>();
        }

        public void SetSystemText(string text)
        {
            if (!(currentBubble is AiSystemBubbleStruct)) CreateSystemBubble();
            currentBubble?.SetText(text);
        }

        public void SetUserText(string text)
        {
            if (!(currentBubble is AiUserBubbleStruct)) CreateUserBubble();
            currentBubble?.SetText(text);
        }

        public void SetAssistantText(string text)
        {
            if (!(currentBubble is AiAssistantBubbleStruct)) CreateAssistantBubble();
            currentBubble?.SetText(text);
        }

        public void SetAssistantReasoningText(string text)
        {
            if (!(currentBubble is AiAssistantBubbleStruct)) CreateAssistantBubble();
            (currentBubble as AiAssistantBubbleStruct)?.SetReasoningText(text);
        }
        #endregion

        #region 流式助手回复
        /// <summary>开始一条助手回复: 创建气泡并清空累积缓冲, 思考区默认隐藏</summary>
        public void BeginAssistantReply()
        {
            assistantContent.Length = 0;
            assistantReasoning.Length = 0;
            hasReasoning = false;
            CreateAssistantBubble();
            (currentBubble as AiAssistantBubbleStruct)?.SetReasoningBubbleActive(false);
        }

        /// <summary>追加正文增量(流式逐段调用)</summary>
        public void AppendAssistantContent(string delta)
        {
            if (string.IsNullOrEmpty(delta)) return;
            assistantContent.Append(delta);
            SetAssistantText(assistantContent.ToString());
        }

        /// <summary>追加思考增量(流式逐段调用); 首个增量到达时自动展开思考区</summary>
        public void AppendAssistantReasoning(string delta)
        {
            if (string.IsNullOrEmpty(delta)) return;
            if (!hasReasoning)
            {
                hasReasoning = true;
                (currentBubble as AiAssistantBubbleStruct)?.SetReasoningBubbleActive(true);
            }
            assistantReasoning.Append(delta);
            SetAssistantReasoningText(assistantReasoning.ToString());
        }
        #endregion

        #region 输入与状态
        /// <summary>提交当前输入框内容(按钮点击 / 回车都会走到这里)</summary>
        public void SubmitInput()
        {
            if (inputField == null) return;
            string text = inputField.text.Trim();
            if (text.Length == 0) return;

            OnSendRequested?.Invoke(text);
            inputField.text = "";
        }

        /// <summary>输入框回车提交(onSubmit 需要 string 参数签名)</summary>
        void OnInputSubmit(string text)
        {
            SubmitInput();
        }

        /// <summary>忙碌态: 请求进行中禁用输入, 防止连点</summary>
        public void SetInputInteractable(bool interactable)
        {
            if (summitBtn != null) summitBtn.interactable = interactable;
            if (inputField != null) inputField.interactable = interactable;
        }

        /// <summary>聚焦输入框(发送后便于连续提问)</summary>
        public void SelectInput()
        {
            if (inputField != null) inputField.Select();
        }

        /// <summary>底部信息条(状态/错误提示)</summary>
        public void SetInfoText(string text)
        {
            if (infoText != null) infoText.text = text;
        }

        /// <summary>滚动到底部(气泡增长时)</summary>
        void ScrollToBottom()
        {
            if (bubbleParent == null) return;
            var scroll = bubbleParent.GetComponentInParent<ScrollRect>();
            if (scroll != null) scroll.verticalNormalizedPosition = 0f;
        }
        #endregion

        #region 工具方法
        T CreateBubble<T>() where T : AiBubbleStructBase
        {
            if (bubbleParent == null) return null;
            AiBubbleStructBase bubble;
            if (typeof(T) == typeof(AiSystemBubbleStruct)) bubble = new AiSystemBubbleStruct(bubbleParent);
            else if (typeof(T) == typeof(AiUserBubbleStruct)) bubble = new AiUserBubbleStruct(bubbleParent);
            else bubble = new AiAssistantBubbleStruct(bubbleParent);

            bubbleList.Add(bubble);
            return bubble as T;
        }
        #endregion
    }
}
