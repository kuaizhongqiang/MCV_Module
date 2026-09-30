using System;
using System.Collections.Generic;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Utils;
using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    /// <summary>步骤答题面板（View）—— 只做展示与输入上报，不做任何判断（一次只出一道题，答对即结束本步骤）。</summary>
    [RequireController(typeof(MCV_Module.Controllers.StepQuestionController))]
    public class StepQuestionPanel : PanelBase
    {
        #region 序列化参数
        [SerializeField] Text contentText;
        [SerializeField] Transform optionsParent;
        [SerializeField] Button submitBtn;
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
        int selectedIndex = -1;               // 已选中的选项下标（-1 = 未选）：纯 UI 状态
        /// <summary>提示文本节点上的组件（TMP 形态下认领的控件是 TMP、Legacy 已被卸载，提示色必须经它下发）。</summary>
        TextComponent m_TipsTextComp;
        /// <summary>题干文本节点上的组件（TMP 形态下节点上的 Legacy Text 被卸载，字段随后成"假 null"，静态入口静默 no-op）。</summary>
        TextComponent m_ContentTextComp;
        #endregion

        #region 事件
        /// <summary>提交一次作答，参数为选中的选项下标（View 只上报「选了谁」，不判断对错）</summary>
        public event Action<int> OnSubmit;
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
            if ((contentText == null && m_ContentTextComp == null) || optionsParent == null || submitBtn == null || (tipsText == null && m_TipsTextComp == null))
            {
                Log.Error("[StepQuestionPanel] 需要手动挂载组件");
                return;
            }
            submitBtn.onClick.AddListener(SubmitClick);
        }

        protected override void OnDestroy()
        {
            // WHY: 布局重建协程由 PanelBase.OnDestroy 统一收尾，这里只管自己的按钮监听
            if (submitBtn != null) submitBtn.onClick.RemoveListener(SubmitClick);
            base.OnDestroy();
        }
        #endregion

        #region 对外接口（只由控制器调用）
        /// <summary>展示一道题：清旧选项 → 复位提示与选中态 → 写题干 → 生成新选项 → 重建布局（题序由控制器决定）。</summary>
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

        /// <summary>提交按钮是否可用（时机由控制器决定：如答对后等待收尾期间禁用）</summary>
        public void SetSubmitInteractable(bool interactable)
        {
            if (submitBtn != null) submitBtn.interactable = interactable;
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

        /// <summary>清空选项：面板复用 / 换题前必须先清，否则选项会不断累积</summary>
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
}
