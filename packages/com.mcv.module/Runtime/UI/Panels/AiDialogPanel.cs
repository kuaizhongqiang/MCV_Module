using System;
using MCV_Module.Utils;
using MCV_Module.Net;
using System.Collections.Generic;
using System.Text;
using MCV_Module.UI.Tools;
using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;
using System.Collections;


namespace MCV_Module.UI.Panels
{
    // WHY: 数据流固定为 输入 -> SubmitInput -> OnSendRequested -> Controller, 回填走 AddUserMessage / BeginAssistantReply / AppendAssistantContent / AppendAssistantReasoning; 流式中途必须纯文本直显, markdown 转换只在 FinalizeAssistantReply 里做一次, 否则未闭合标记会闪烁错乱
    /// <summary>AI 对话面板 —— 只负责展示与输入, 不碰 AI 逻辑。</summary>
    public class AiDialogPanel : PanelBase
    {
        readonly List<AiBubbleStructBase> bubbleList = new List<AiBubbleStructBase>();
        readonly StringBuilder assistantContent = new StringBuilder();
        readonly StringBuilder assistantReasoning = new StringBuilder();

        [SerializeField] Transform bubbleParent;
        [SerializeField] InputField inputField;
        [SerializeField] Button summitBtn;
        [SerializeField] Text infoText;
        [SerializeField] Transform modelSwitchToggleParent;
        [SerializeField] Button modelSwitchBtn;
        // WHY: 新字段直接用 TextComponent（不是 Legacy Text）—— 文本的唯一写入路径是 TextComponent；TMP 形态下它会卸载节点上的
        //      Legacy Text，于是"持有 Text 字段"的写法随成"假 null"、静态入口静默 no-op。未绑定时按按钮下的文本组件兜底。
        [SerializeField] TextComponent modelSwitchBtnText;
        [SerializeField] RectTransform movePanel;
        [SerializeField] float closeXValue = -600f;
        [SerializeField] float openXValue = 135f;

        /// <summary>底部信息条节点上的组件（TMP 形态下节点上的 Legacy Text 被卸载，字段随后成"假 null"，静态入口静默 no-op）。</summary>
        TextComponent m_InfoTextComp;

        AiBubbleStructBase currentBubble;
        bool hasReasoning;
        // WHY: 预制体已移出 Resources 进 UI 包，这里存裸 prefab 名（UIPrefabUtil 按 ui_{name} 拼包配置 id）。
        const string ToggleListModelPrefabName = "ModelListToggle";
        // WHY: 列表项是独立碎片 prefab（ui_ModelListToggle），面板无法反向引用它的字段，只能按节点名取它的两行文字
        const string ModelProviderTextNodeName = "ProviderText";
        const string ModelNameTextNodeName = "ModelNameText";
        // WHY: 面板开关按钮（AiSwitchBtn）的文案 —— 真 key 是 ui.ai.title（LanguageData 里已有"AI 对话 / AI Chat"；
        //      预制体上挂的 ui.TaskNameText 是迁移工具生成的假 key，取不到会告警 + 只显示中文字面量）
        const string ModelSwitchBtnTextKey = "ui.ai.title";
        /// <summary>列表项身份 → Toggle（回勾当前模型用）；身份 = "provider - model"。</summary>
        readonly Dictionary<string, Toggle> toggleDict = new Dictionary<string, Toggle>();
        /// <summary>列表项 → provider 名（用户点选时回传用）。</summary>
        readonly Dictionary<Toggle, string> toggleProvider = new Dictionary<Toggle, string>();
        /// <summary>列表项 → model 名（用户点选时回传用）。</summary>
        readonly Dictionary<Toggle, string> toggleModel = new Dictionary<Toggle, string>();

        /// <summary>程序化回勾中：为 true 时 toggle 的变化不算"用户选择"，避免回勾又触发一次选择事件。</summary>
        bool suppressToggleEvent;
        /// <summary>当前模型的镜像（真源在 Controller）；只用于按钮文字与回勾。</summary>
        string selectedProvider = string.Empty;
        string selectedModel = string.Empty;

        /// <summary>用户提交输入时触发(携带文本), 由 Controller 订阅。先清后加, 避免重复订阅。</summary>
        public event Action<string> OnSendRequested;

        /// <summary>用户选定模型时触发(provider, model), 由 Controller 订阅；面板只上报"选了哪个", 不发请求。</summary>
        public event Action<string, string> OnModelSelected;

        /// <summary>用户按开关按钮展开面板主体时触发, 由 Controller 订阅（模型目录没取到时再试一次）。</summary>
        public event Action OnPanelOpened;

        /// <summary>是否已有消息(用于首次展示欢迎语)</summary>
        public bool HasMessage { get { return bubbleList.Count > 0; } }

        Coroutine panelAnimCoroutine;
        CanvasGroup moveCanvasGroup;
        readonly float duration = 0.3f;

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

            // WHY: 尽早解析并缓存 —— TMP 形态下本组件会卸载节点上的 Legacy Text，之后 infoText 成了"假 null"，
            // 那时再 GetComponent 会抛、静态入口也会静默 no-op；持有组件则赋值时机随意（装配未完成会先进 pending 缓冲）。
            if (infoText != null) m_InfoTextComp = infoText.GetComponent<TextComponent>();

            // WHY: 开关按钮先于 bubbleParent 的早退绑定 —— 气泡父节点缺失不该把面板开合一并弄成死的
            if (modelSwitchBtn != null)
            {
                modelSwitchBtn.onClick.RemoveListener(OnModelSwitchClicked);
                modelSwitchBtn.onClick.AddListener(OnModelSwitchClicked);
            }

            // WHY: 按钮文案走真语言 key —— 预制体上那个 key 是迁移工具按节点名生成的假 key（LanguageData 里没有它），
            //      每次 Awake 都会告警并回退成本地字面量，中英切换也随之失效
            TextComponent switchLabel = ResolveModelSwitchTextComp();
            if (switchLabel != null) switchLabel.SetTextKey(ModelSwitchBtnTextKey);

            if (bubbleParent == null)
            {
                Log.Error("AiDialogPanel: 需要手动挂载 bubbleParent！");
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
            if (modelSwitchBtn != null)
                modelSwitchBtn.onClick.RemoveListener(OnModelSwitchClicked);
            ClearModelList();
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

        /// <summary>设置助手正文(纯文本, 流式中途用, 不做 markdown 转换)。</summary>
        public void SetAssistantTextPlain(string text)
        {
            if (!(currentBubble is AiAssistantBubbleStruct)) CreateAssistantBubble();
            (currentBubble as AiAssistantBubbleStruct)?.SetTextPlain(text);
        }

        public void SetAssistantReasoningText(string text)
        {
            if (!(currentBubble is AiAssistantBubbleStruct)) CreateAssistantBubble();
            (currentBubble as AiAssistantBubbleStruct)?.SetReasoningText(text);
        }

        /// <summary>设置助手思考(纯文本, 流式中途用, 不做 markdown 转换)。</summary>
        public void SetAssistantReasoningTextPlain(string text)
        {
            if (!(currentBubble is AiAssistantBubbleStruct)) CreateAssistantBubble();
            (currentBubble as AiAssistantBubbleStruct)?.SetReasoningTextPlain(text);
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

        /// <summary>追加正文增量(流式中途逐段调用)：只用纯文本显示, 不做 markdown 转换, 完成后由 <see cref="FinalizeAssistantReply"/> 一次性转换。</summary>
        public void AppendAssistantContent(string delta)
        {
            if (string.IsNullOrEmpty(delta)) return;
            assistantContent.Append(delta);
            SetAssistantTextPlain(assistantContent.ToString());
        }

        // WHY: 只有**非空白**的思考内容才展开思考区 —— 有的 provider（如豆包）首帧会回一个纯 "\n" 的 reasoning 分片，
        //      照 old 逻辑 IsNullOrEmpty 过滤不掉它，于是"没有思考输出"也会冒出一个空思考气泡。
        /// <summary>追加思考增量(流式中途逐段调用, 纯文本); 首个非空白增量到达时自动展开思考区</summary>
        public void AppendAssistantReasoning(string delta)
        {
            if (string.IsNullOrWhiteSpace(delta)) return;
            if (!hasReasoning)
            {
                hasReasoning = true;
                (currentBubble as AiAssistantBubbleStruct)?.SetReasoningBubbleActive(true);
            }
            assistantReasoning.Append(delta);
            SetAssistantReasoningTextPlain(assistantReasoning.ToString());
        }

        // WHY: 这是唯一的 markdown 转换时机, 必须等流式输出全部结束; 中途转换会因标记不完整而错乱/闪烁
        /// <summary>流式完成：对累积的完整正文/思考一次性做 markdown → RichText 转换并设置。</summary>
        public void FinalizeAssistantReply()
        {
            var assistantBubble = currentBubble as AiAssistantBubbleStruct;
            if (assistantBubble == null) return;

            // 正文: 累积的完整内容一次转换
            if (assistantContent.Length > 0)
            {
                SetAssistantText(assistantContent.ToString());
            }

            // 思考: 只有非空白内容才写入并保留思考区；否则收起来（纯空白分片不该留下一颗空气泡）
            string reasoning = assistantReasoning.ToString();
            if (!string.IsNullOrWhiteSpace(reasoning))
            {
                SetAssistantReasoningText(reasoning);
            }
            else
            {
                assistantBubble.SetReasoningBubbleActive(false);
            }
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
            if (m_InfoTextComp != null) m_InfoTextComp.SetText(text);
            else TextComponent.SetTextOn(infoText, text);
        }

        /// <summary>滚动到底部(气泡增长时)</summary>
        void ScrollToBottom()
        {
            if (bubbleParent == null) return;
            var scroll = bubbleParent.GetComponentInParent<ScrollRect>();
            if (scroll != null) scroll.verticalNormalizedPosition = 0f;
        }
        #endregion

        #region 模型切换
        // WHY: 模型切换有多种事件来源, 表现/交互层与逻辑层必须分开 —— 用户点选走 OnModelToggleChanged(抛事件), 外部(Controller)同步走 SetModelToggle(静默)
        /// <summary>同步当前模型（回勾列表项），不抛选择事件；由 Controller 调用。</summary>
        public void ApplyCurrentModel(string provider, string model)
        {
            if (string.IsNullOrEmpty(model)) return;
            selectedProvider = provider ?? string.Empty;
            selectedModel = model;

            Toggle toggle = FindModelToggle(selectedProvider, selectedModel);
            if (toggle != null) SetModelToggle(toggle, true);
        }

        /// <summary>模型列表（ProviderPart）显隐；展开时补一次布局重建。</summary>
        public void SetModelListVisible(bool visible)
        {
            Transform root = ModelListRoot();
            if (root == null) return;
            root.gameObject.SetActive(visible);
            if (visible) RequestLayoutRebuild(modelSwitchToggleParent);
        }

        /// <summary>装配模型列表（清旧 → 按 provider × model 建项 → 回勾当前模型）；由 Controller 取到目录后调用。</summary>
        public void BuildModelList(AiModelsResult result)
        {
            ClearModelList();
            if (result == null || result.providers == null) return;

            for (int i = 0; i < result.providers.Length; i++)
            {
                AiModelProviderInfo provider = result.providers[i];
                if (provider == null || provider.models == null) continue;

                for (int k = 0; k < provider.models.Length; k++)
                {
                    string model = provider.models[k];
                    if (string.IsNullOrEmpty(model)) continue;

                    Toggle toggle = CreateModelListToggle(provider.name, model);
                    if (toggle == null) continue;

                    toggleDict[ModelKey(provider.name, model)] = toggle;
                    toggleProvider[toggle] = provider.name ?? string.Empty;
                    toggleModel[toggle] = model;
                    // WHY: lambda 捕获的是每轮循环各自的局部变量（toggle 在循环体内声明），不会串项
                    toggle.onValueChanged.AddListener(isOn => OnModelToggleChanged(toggle, isOn));
                }
            }

            Log.Info($"[AiDialogPanel] 模型列表已装配：{toggleDict.Count} 项（默认 provider = {result.defaultProvider}）");
            if (!string.IsNullOrEmpty(selectedModel)) ApplyCurrentModel(selectedProvider, selectedModel);
            RequestLayoutRebuild(modelSwitchToggleParent);
        }

        /// <summary>程序化勾选/取消列表项（静默：不抛 OnModelSelected，也不会再触发一次选择事件）。</summary>
        public void SetModelToggle(Toggle toggle, bool isOn)
        {
            if (toggle == null) return;
            suppressToggleEvent = true;
            toggle.isOn = isOn;
            if (isOn) TurnOffOthers(toggle);
            suppressToggleEvent = false;
        }

        // WHY: 列表项是"单选"—— 用户把当前项点掉不算有效选择, 静默勾回去, 否则会出现一个都没选的空态
        /// <summary>用户点选列表项：互斥 → 记住选择 → 更新按钮文字 → 收起列表 → 上报 Controller。</summary>
        void OnModelToggleChanged(Toggle toggle, bool isOn)
        {
            if (suppressToggleEvent || toggle == null) return;

            if (!isOn)
            {
                SetModelToggle(toggle, true);
                return;
            }

            TurnOffOthers(toggle);

            string provider = string.Empty;
            string model;
            toggleProvider.TryGetValue(toggle, out provider);
            if (!toggleModel.TryGetValue(toggle, out model)) return;

            selectedProvider = provider ?? string.Empty;
            selectedModel = model;
            SetModelListVisible(false);
            Log.Info($"[AiDialogPanel] 已选择模型：{selectedProvider} / {selectedModel}");
            OnModelSelected?.Invoke(selectedProvider, selectedModel);
        }

        /// <summary>按下开关按钮（AiSwitchBtn）：开合面板主体（Shape）；展开时通知 Controller 补一次模型目录。</summary>
        void OnModelSwitchClicked()
        {
            bool willOpen = !IsPanelActive;
            SetPanelActive(willOpen);
            if (willOpen) OnPanelOpened?.Invoke();
        }

        // WHY: 列表根是 ProviderPart、而 Inspector 绑的是它里面的 Layout —— 面板不主动开合它（按钮管的是 Shape），
        //      这个方法留给"谁来开列表"的调用方；显隐只能作用于根节点，所以取"Layout 的父节点"，不再加 prefab 字段（改 prefab 要重打 UI 包）。
        /// <summary>模型列表根节点（ProviderPart）；未绑定时返回 null。</summary>
        Transform ModelListRoot()
        {
            if (modelSwitchToggleParent == null) return null;
            return modelSwitchToggleParent.parent != null ? modelSwitchToggleParent.parent : modelSwitchToggleParent;
        }

        // WHY: 列表根上没有 ToggleGroup（碎片 prefab 只有一个 Toggle），单选互斥必须自己关，否则会同时亮多个
        /// <summary>把除 keep 以外的列表项全部关掉。</summary>
        void TurnOffOthers(Toggle keep)
        {
            foreach (KeyValuePair<string, Toggle> pair in toggleDict)
            {
                Toggle other = pair.Value;
                if (other != null && other != keep && other.isOn) other.isOn = false;
            }
        }

        /// <summary>按 (provider, model) 找列表项；provider 为空时只按 model 找。</summary>
        Toggle FindModelToggle(string provider, string model)
        {
            if (string.IsNullOrEmpty(model)) return null;

            if (!string.IsNullOrEmpty(provider))
            {
                Toggle exact;
                if (toggleDict.TryGetValue(ModelKey(provider, model), out exact)) return exact;
            }

            foreach (KeyValuePair<Toggle, string> pair in toggleModel)
            {
                if (pair.Value == model) return pair.Key;
            }
            return null;
        }

        /// <summary>清空模型列表（退订 → 清索引 → 销毁列表项）。</summary>
        void ClearModelList()
        {
            foreach (KeyValuePair<string, Toggle> pair in toggleDict)
            {
                if (pair.Value != null) pair.Value.onValueChanged.RemoveAllListeners();
            }
            toggleDict.Clear();
            toggleProvider.Clear();
            toggleModel.Clear();
            if (modelSwitchToggleParent != null) ClearChildren(modelSwitchToggleParent);
        }

        /// <summary>列表项身份：provider 与 model 拼成的稳定键（也是列表项节点名）。</summary>
        static string ModelKey(string provider, string model)
        {
            return (provider ?? string.Empty) + " - " + model;
        }

        // WHY: 按钮文字节点没在 Inspector 绑定时自动取按钮下的文本组件 —— 预制体不必为一个新字段重绑（改了 prefab 还得重打 UI 包）；
        //      按钮下有多个文本节点时必须显式绑定，否则可能写错节点。
        /// <summary>模型切换按钮的文本组件（未绑定时按按钮下第一个文本组件兜底）。</summary>
        TextComponent ResolveModelSwitchTextComp()
        {
            if (modelSwitchBtnText != null) return modelSwitchBtnText;
            if (modelSwitchBtn != null) modelSwitchBtnText = modelSwitchBtn.GetComponentInChildren<TextComponent>(true);
            return modelSwitchBtnText;
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

        // WHY: 列表项文字必须走 TextComponent（节点上是 Legacy Text + 组件；TMP 形态下组件会把 Legacy 卸载，直写 Text 会写空）
        /// <summary>实例化一个模型列表项并填两行文字（provider / model）；返回它的 Toggle。</summary>
        Toggle CreateModelListToggle(string providerName, string modelName)
        {
            if (modelSwitchToggleParent == null) return null;

            GameObject prefab = UIPrefabUtil.Get(ToggleListModelPrefabName);
            if (prefab == null) return null;

            GameObject go = Instantiate(prefab, modelSwitchToggleParent);
            go.name = ModelKey(providerName, modelName);

            Toggle toggle = go.GetComponent<Toggle>();
            if (toggle == null)
            {
                Log.Error($"[AiDialogPanel] {ToggleListModelPrefabName} 上没有 Toggle，模型列表项无法使用");
                return null;
            }

            SetItemText(go, ModelProviderTextNodeName, providerName);
            SetItemText(go, ModelNameTextNodeName, modelName);
            // WHY: 碎片 prefab 上的 isOn 初值不可信（可能被美术勾上），列表项一律从"未选"起；必须在挂回调之前置位，否则会假触发一次选择
            toggle.isOn = false;

            return toggle;
        }

        /// <summary>写列表项某一行文字（节点上有 TextComponent 走组件，退化为 Legacy 直写）。</summary>
        static void SetItemText(GameObject item, string nodeName, string value)
        {
            Transform node = item.transform.Find(nodeName);
            if (node == null)
            {
                Log.Warning($"[AiDialogPanel] 模型列表项缺少节点「{nodeName}」，该项文字未设置");
                return;
            }

            TextComponent comp = node.GetComponent<TextComponent>();
            if (comp != null)
            {
                comp.SetText(value);
                return;
            }

            Text legacy = node.GetComponent<Text>();
            if (legacy != null) TextComponent.SetTextOn(legacy, value);
        }
        #endregion
    
        #region Panel主体显示隐藏
        // WHY: 面板主体 = Shape（movePanel）；开关按钮 AiSwitchBtn 在它外面（x=25，Shape 从 135 起），所以收起后按钮仍在，能再点开
        /// <summary>面板主体当前是否展开（按锚点判断：动画与立即版都把位置与 alpha 一起写，位置是权威值）。</summary>
        public bool IsPanelActive
        {
            get { return movePanel != null && Mathf.Abs(movePanel.anchoredPosition.x - openXValue) < 1f; }
        }

        /// <summary>开合面板主体（带滑动 / 淡入淡出动画）。</summary>
        public void SetPanelActive(bool isActive)
        {
            SetPanelActiveAnim(isActive);
        }

        /// <summary>开合面板主体（无动画，立即到位）；用于面板刚创建时先摆到"收起"位。</summary>
        public void SetPanelActiveImmediately(bool isActive)
        {
            // WHY: 先停掉在跑的动画 —— 否则旧协程会继续按自己的目标改锚点，与这次的立即赋值互相覆盖（表现为"闪一下又滑回去"）
            StopPanelAnim();

            if (movePanel == null)
            {
                Log.Warning("[AiDialogPanel] movePanel 未绑定，面板主体位移被跳过");
                return;
            }

            float targetAlpha = isActive ? 1f : 0f;
            float targetX = isActive ? openXValue : closeXValue;
            EnsureMoveCanvasGroup();
            movePanel.anchoredPosition = new Vector2(targetX, movePanel.anchoredPosition.y);
            moveCanvasGroup.alpha = targetAlpha;
            SetModelSwitchInteractable(true);
        }

        void SetPanelActiveAnim(bool isActive)
        {
            if (movePanel == null)
            {
                Log.Warning("[AiDialogPanel] movePanel 未绑定，面板主体动画被跳过");
                return;
            }

            EnsureMoveCanvasGroup();
            StopPanelAnim();
            panelAnimCoroutine = StartCoroutine(SetPanelActiveAnimCoroutine(isActive));
        }

        // WHY: 先 GetComponent 再 AddComponent —— AddComponent 不检查重复，节点上本来就有一个 CanvasGroup 时会挂出第二个（多一份布局开销）
        /// <summary>确保 movePanel 上有用于淡入淡出的 CanvasGroup。</summary>
        void EnsureMoveCanvasGroup()
        {
            if (moveCanvasGroup != null) return;
            moveCanvasGroup = movePanel.GetComponent<CanvasGroup>();
            if (moveCanvasGroup == null) moveCanvasGroup = movePanel.gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>停掉进行中的主体动画（立即版与再次触发都要先停）。</summary>
        void StopPanelAnim()
        {
            if (panelAnimCoroutine == null) return;
            StopCoroutine(panelAnimCoroutine);
            panelAnimCoroutine = null;
        }

        /// <summary>动画期间禁掉开关按钮，避免动画中途被连点（两次点击会互相抢目标位置）。</summary>
        void SetModelSwitchInteractable(bool interactable)
        {
            if (modelSwitchBtn != null) modelSwitchBtn.interactable = interactable;
        }

        IEnumerator SetPanelActiveAnimCoroutine(bool isActive)
        {
            SetModelSwitchInteractable(false);
            float currentAlpha = moveCanvasGroup.alpha;
            float currentX = movePanel.anchoredPosition.x;
            float targetAlpha = isActive ? 1f : 0f;
            float targetX = isActive ? openXValue : closeXValue;
            float time = 0;

            while (time < duration)
            {
                time += Time.deltaTime;
                moveCanvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, time / duration);
                movePanel.anchoredPosition = new Vector2(Mathf.Lerp(currentX, targetX, time / duration), movePanel.anchoredPosition.y);
                yield return null;
            }
            moveCanvasGroup.alpha = targetAlpha;
            movePanel.anchoredPosition = new Vector2(targetX, movePanel.anchoredPosition.y);

            SetModelSwitchInteractable(true);

            panelAnimCoroutine = null;
        }
        #endregion
    }
}
