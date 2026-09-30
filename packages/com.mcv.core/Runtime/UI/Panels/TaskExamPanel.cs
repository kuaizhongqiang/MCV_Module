using System;
using System.Collections.Generic;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Utils;
using UnityEngine;
using MCV_Module.UI.Components;
using MCV_Module.UI.Tools;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    /// <summary>考核（Exam）面板（View）—— 只做展示与输入上报，不做任何判断；题目流程全部由 TaskExamController 驱动。</summary>
    [RequireController(typeof(MCV_Module.Controllers.TaskExamController))]
    public class TaskExamPanel : PanelBase
    {
        #region 序列化参数
        [SerializeField] GameObject companyText;
        [SerializeField] GameObject copyrightText;
        [SerializeField] Text contentText;
        [SerializeField] Transform optionsParent;
        [SerializeField] Button submitBtn;
        [SerializeField] Button backBtn;
        [SerializeField] Text tipsText;
        [SerializeField] Color rightTipsColor = Color.green;
        [SerializeField] Color wrongTipsColor = Color.red;
        [SerializeField] AudioEffectType rightAudio = AudioEffectType.Success;
        [SerializeField] AudioEffectType wrongAudio = AudioEffectType.Fail;
        #endregion

        #region 参数
        const string RightTips = "恭喜，回答正确！";
        const string WrongTips = "回答错误！再试一次吧！";
        const string NoSelectedTips = "请先选择一个选项";
        readonly List<TaskExamOptionsToggle> options = new List<TaskExamOptionsToggle>(); // 当前题的选项
        int selectedIndex = -1;               // 当前题已选中的选项下标（-1 = 未选）：纯 UI 状态
        /// <summary>提示文本节点上的组件（TMP 形态下认领的控件是 TMP、Legacy 已被卸载，提示色必须经它下发）。</summary>
        TextComponent m_TipsTextComp;
        /// <summary>题干文本节点上的组件（TMP 形态下节点上的 Legacy Text 被卸载，字段随后成"假 null"，静态入口静默 no-op）。</summary>
        TextComponent m_ContentTextComp;
        #endregion

        #region 事件
        /// <summary>提交一次作答，参数为选中的选项下标（View 只上报「选了谁」，不判断对错）</summary>
        public event Action<int> OnSubmit;

        /// <summary>返回入口点击（回菜单页，换页决策由 TaskExamController 做）</summary>
        public event Action OnBackClick;
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();   // WHY: canvasGroup 在 UIBase.Awake 里初始化，必须调用

            // WHY: 提前解析一次组件 —— 必须早于卸载：换形态在本组件 Awake 里发起，而 Destroy 到帧末才生效，此刻 GetComponent
            // 稳定可用，换形态之后再解析就会抛。TMP 形态下本组件会把该节点上的 Legacy Text **卸载**（先禁用再 Destroy；必须卸 ——
            // Unity 不允许同一个 GameObject 上存在两个 Graphic，留着 Legacy 会让 AddComponent<TextMeshProUGUI>() 被拒绝并返回 null），
            // 并把当前认领的控件换成 TMP；直写 tipsText.color 会抛 MissingReferenceException，提示色只能写组件的 ColorValue。
            if (tipsText != null) m_TipsTextComp = tipsText.GetComponent<TextComponent>();

            // WHY: 题干同样要提前解析 —— 换过形态后 contentText 成了"假 null"，静态入口 SetTextOn 会被静默丢弃（题干空白）。
            if (contentText != null) m_ContentTextComp = contentText.GetComponent<TextComponent>();

            // WHY: 组件存在即视为已配置 —— 换形态后 Legacy 被卸载、Text 字段变成 null 是正常状态，只有字段与缓存组件都为 null 才算缺配置。
            if ((contentText == null && m_ContentTextComp == null) || optionsParent == null || submitBtn == null || backBtn == null
                || companyText == null || copyrightText == null || (tipsText == null && m_TipsTextComp == null))
            {
                Log.Error("[TaskExamPanel] 需要手动挂载组件");
                return;
            }
            submitBtn.onClick.AddListener(SubmitClick);
            backBtn.onClick.AddListener(BackClick);
        }

        protected override void OnDestroy()
        {
            // WHY: 布局重建协程由 PanelBase.OnDestroy 统一收尾，这里只管自己的按钮监听
            if (submitBtn != null) submitBtn.onClick.RemoveListener(SubmitClick);
            if (backBtn != null) backBtn.onClick.RemoveListener(BackClick);
            base.OnDestroy();
        }
        #endregion

        #region 对外接口（只由 Controller 调用）
        /// <summary>展示一道题：清旧选项 → 复位提示与选中态 → 写题干 → 生成新选项 → 重建布局（题序/选项顺序由控制器决定）。</summary>
        public void ShowQuestion(QuestionClip clip)
        {
            ClearOptions();
            ClearTips();
            selectedIndex = -1;
            SetSubmitInteractable(true);

            if (clip == null) return;
            string question = Localized.Pick(clip.questionText, clip.questionTextEn);
            if (m_ContentTextComp != null) m_ContentTextComp.SetText(question);
            else TextComponent.SetTextOn(contentText, question);
            if (clip.options != null)
            {
                for (int i = 0; i < clip.options.Count; i++)
                {
                    // WHY: 只把「文案」交给选项——对错信息不进 View，判题归控制器
                    options.Add(new TaskExamOptionsToggle(Localized.Pick(clip.options[i].itemText, clip.options[i].itemTextEn), i, optionsParent, SelectOption));
                }
            }
            // WHY: 选项是运行时生成的，父级 LayoutGroup/ContentSizeFitter 必须等子级尺寸算完 —— 统一交给 RequestLayoutRebuild
            //      （等一帧 + 按深度自下而上 + 防重入）
            RequestLayoutRebuild();
        }

        /// <summary>展示作答结果：提示文案 + 配色 + 对错音效（对错由控制器判定后传进来）</summary>
        public void ShowResult(bool isRight)
        {
            SetTips(isRight ? RightTips : WrongTips, isRight ? rightTipsColor : wrongTipsColor);
            GlobalAudioMgr.PlayAudio(isRight ? rightAudio : wrongAudio);
        }

        /// <summary>提交按钮是否可用（时机由控制器决定：如答对后等待出题期间禁用）</summary>
        public void SetSubmitInteractable(bool interactable)
        {
            if (submitBtn != null) submitBtn.interactable = interactable;
        }

        // WHY: 开关的唯一源是 GlobalUIMgr.IfCopyright / IfCompany（启动时按加密内容写入），面板不自己读数据
        /// <summary>版权信息显隐：公司名与版权声明由控制器按全局开关装配（缺引用时已由 Awake 拦截）。</summary>
        public void SetCopyright(bool ifCopyright, bool ifCompany)
        {
            companyText.SetActive(ifCompany);
            copyrightText.SetActive(ifCopyright);
        }

        // WHY: 结束表现刻意留空，不再走「清空选项 + 显示结束文案」的旧做法，收尾表现后续在此补
        /// <summary>全部题目作答完成（控制器判定后调用）。</summary>
        public void ShowFinish()
        {
            // TODO: 考核结束的收尾表现（留空，后续填写）
        }
        #endregion

        #region 私有方法（纯表现）
        /// <summary>提交按钮：未选只本地提示；已选则把下标抛给控制器（不在这里判题）</summary>
        void SubmitClick()
        {
            if (selectedIndex < 0 || selectedIndex >= options.Count)
            {
                SetTips(NoSelectedTips, wrongTipsColor);
                return;
            }
            OnSubmit?.Invoke(selectedIndex);
        }

        /// <summary>返回按钮：把「返回」抛给控制器（不在这里换页）</summary>
        void BackClick() => OnBackClick?.Invoke();

        /// <summary>选项选中回调：单选互斥（再次点击同一项 = 取消选择）</summary>
        void SelectOption(int index, bool isOn)
        {
            if (isOn)
            {
                selectedIndex = index;
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].index != index) options[i].SetOnWithoutNotify(false);
                }
            }
            else if (selectedIndex == index)
            {
                selectedIndex = -1;
            }
        }

        /// <summary>清空选项：面板复用 / 切题前必须先清，否则选项会不断累积</summary>
        void ClearOptions()
        {
            for (int i = 0; i < options.Count; i++) options[i].Dispose();
            options.Clear();
        }

        void SetTips(string text, Color color)
        {
            if (tipsText == null && m_TipsTextComp == null) return;
            // WHY: 文本同样要经组件写 —— 换形态后 tipsText 成了"假 null"，静态入口会静默 no-op（提示文案不更新）。
            if (m_TipsTextComp != null) m_TipsTextComp.SetText(text);
            else TextComponent.SetTextOn(tipsText, text);
            // WHY: 优先写组件的 ColorValue 而不是直写 tipsText.color —— 换形态后 tipsText 成了"假 null"（Legacy 已被卸载），
            // 直写会抛 MissingReferenceException；可见控件是 TMP，ColorValue 由 ApplyStyle 下发到当前认领的控件。
            // 仅节点上本就没有组件时才退回 SetColorOn 直写。
            if (m_TipsTextComp != null) m_TipsTextComp.ColorValue = color;
            else TextComponent.SetColorOn(tipsText, color);
        }

        void ClearTips() => SetTips(string.Empty, Color.clear);
        #endregion
    }

    /// <summary>考核选项条目：Toggle + 序号（A/B/C/D）+ 选项文案，只认文案不认对错（正确性由控制器持在题目数据里）。</summary>
    public class TaskExamOptionsToggle
    {
        // WHY: 选项预制体已移出 Resources 进 UI 包，这里存裸 prefab 名（UIPrefabUtil 按 ui_{name} 拼包配置 id）。
        const string OptionPrefabName = "ExamOptionsToggle";

        public Toggle toggle;
        public Text contentText;
        public Text indexText;
        public int index;

        // WHY: 选项行是运行时 Instantiate 出来的，两个文本节点没有"面板字段"可缓存，只能在创建时同一处把组件取出来存住 ——
        // TMP 形态下 TextComponent 会卸载节点上的 Legacy Text，contentText / indexText 随后成"假 null"，静态入口静默 no-op。
        TextComponent contentTextComp;
        TextComponent indexTextComp;

        readonly Action<int, bool> onValueChanged;
        GameObject go;

        public TaskExamOptionsToggle(string itemText, int index, Transform parent, Action<int, bool> onValueChanged)
        {
            this.index = index;
            this.onValueChanged = onValueChanged;
            CreateToggle(itemText, index, parent);
        }

        void CreateToggle(string itemText, int index, Transform parent)
        {
            GameObject prefab = UIPrefabUtil.Get(OptionPrefabName);
            if (prefab == null)
            {
                Log.Warning($"[TaskExamOptionsToggle] 缺少选项预制体: {OptionPrefabName}");
                return;
            }

            go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = "Option_" + index;

            toggle = go.GetComponent<Toggle>();
            if (toggle == null)
            {
                Log.Warning("[TaskExamOptionsToggle] 选项预制体根节点缺少 Toggle 组件");
                return;
            }

            Transform marks = go.transform.childCount > 0 ? go.transform.GetChild(0) : null;
            Transform option = go.transform.childCount > 1 ? go.transform.GetChild(1) : null;
            Transform indexNode = marks != null && marks.childCount > 1 ? marks.GetChild(1) : null;
            indexText = indexNode != null ? indexNode.GetComponent<Text>() : null;
            contentText = option != null ? option.GetComponent<Text>() : null;
            // WHY: 组件经**节点**取而不是经 Text 字段 —— 换形态后该字段是"假 null"，对它调 GetComponent 会抛。
            indexTextComp = indexNode != null ? indexNode.GetComponent<TextComponent>() : null;
            contentTextComp = option != null ? option.GetComponent<TextComponent>() : null;

            // WHY: 判"层级不符合预期"要把组件也算上 —— TMP 形态下 Legacy 已被卸载、Text 字段为"假 null"，只看字段会误报。
            if ((contentText == null && contentTextComp == null) || (indexText == null && indexTextComp == null))
            {
                Log.Warning("[TaskExamOptionsToggle] 选项预制体层级不符合预期（需要 Marks/CountText 与 OptionText）");
            }

            string indexLabel = GetIndexChar(index) + ": ";
            if (contentTextComp != null) contentTextComp.SetText(itemText);
            else if (contentText != null) TextComponent.SetTextOn(contentText, itemText);
            if (indexTextComp != null) indexTextComp.SetText(indexLabel);
            else if (indexText != null) TextComponent.SetTextOn(indexText, indexLabel);

            toggle.isOn = false;
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        void OnToggleChanged(bool isOn) => onValueChanged?.Invoke(index, isOn);

        /// <summary>外部同步选中状态且不触发回调（单选互斥用）</summary>
        public void SetOnWithoutNotify(bool isOn)
        {
            if (toggle != null) toggle.SetIsOnWithoutNotify(isOn);
        }

        /// <summary>销毁选项：切题 / 面板重建时必须调用，避免监听残留与选项累积</summary>
        public void Dispose()
        {
            if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggleChanged);
            toggle = null;
            contentText = null;
            indexText = null;
            contentTextComp = null;
            indexTextComp = null;
            if (go != null) UnityEngine.Object.Destroy(go);
            go = null;
        }

        string GetIndexChar(int index)
        {
            // 返回ABCD
            return ((char)('A' + index)).ToString();
        }
    }
}
