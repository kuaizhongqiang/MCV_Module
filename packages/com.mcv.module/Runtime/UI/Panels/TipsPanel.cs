using System;
using System.Collections.Generic;
using MCV_Module.Controllers;
using MCV_Module.UI.Tools;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    // WHY: 面板随 Canvas 重建、自身不保存跨状态数据；业务侧不要直接调本类填内容，走 TipsController.SetStepTips / SetOpTips / SetTips
    /// <summary>提示条面板（View）—— 步骤/操作两套双缓冲提示，只负责显示与交互采集，不读数据、不做流程决策。</summary>
    [RequireController(typeof(TipsController))]
    public class TipsPanel : PanelBase
    {
        // 双缓冲键：与 clone 的物体名保持一致，便于在 Hierarchy 里对照
        const string StepSerializedKey = "StepTipsContent";
        const string StepCloneKey = "StepContentClone";
        const string OpSerializedKey = "OpTipsContent";
        const string OpCloneKey = "OpContentClone";

        [SerializeField] RectTransform StepTipsContent; // 步骤提示的序列化对象（始终作为显示状态，由 AnimSwap 保证）
        [SerializeField] RectTransform OpTipsContent;   // 操作提示的序列化对象（同上）
        [SerializeField] Toggle StepPartSwitchToggle;   // 步骤提示部分的开关
        [SerializeField] Toggle OpPartSwitchToggle;     // 操作提示部分的开关
        [SerializeField] float MoveDuration = 0.5f;     // 内容切换位移动画时长
        [SerializeField] float ToggleTimerDuration = 3f;// 开关自动收起时长（<=0 表示不自动收起）

        readonly Dictionary<string, TipsContentUtiliy> stepDict = new Dictionary<string, TipsContentUtiliy>();
        readonly Dictionary<string, TipsContentUtiliy> opDict = new Dictionary<string, TipsContentUtiliy>();
        int stepAnimVersion; // 步骤动画版本计数器，快速切换时丢弃过时回调
        int opAnimVersion;   // 操作动画版本计数器
        bool m_Ready;        // 关键引用齐备且双缓冲已就绪

        /// <summary>步骤提示开关被用户改变（由 Controller 订阅，先清后加）。</summary>
        public event Action<bool> OnStepPartToggled;
        /// <summary>操作提示开关被用户改变（由 Controller 订阅，先清后加）。</summary>
        public event Action<bool> OnOpPartToggled;

        /// <summary>自动收起时长（配置在 prefab；是否启用、什么任务下启用由 Controller 决策）。</summary>
        public float AutoHideDelay => ToggleTimerDuration;

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();

            if (StepTipsContent == null || OpTipsContent == null || StepPartSwitchToggle == null || OpPartSwitchToggle == null)
            {
                Log.Error("[TipsPanel] 缺少必要组件（StepTipsContent / OpTipsContent / StepPartSwitchToggle / OpPartSwitchToggle 都需要挂载）", this);
                return;
            }

            stepDict.Add(StepSerializedKey, new TipsContentUtiliy(StepTipsContent.transform, StepTipsContent.gameObject, MoveDuration, this));
            opDict.Add(OpSerializedKey, new TipsContentUtiliy(OpTipsContent.transform, OpTipsContent.gameObject, MoveDuration, this));

            // clone 作为双缓冲备用对象，只在播入场动画时临时打开
            GameObject stepClone = Instantiate(StepTipsContent.gameObject, StepTipsContent.parent);
            stepClone.name = StepCloneKey;
            stepDict.Add(StepCloneKey, new TipsContentUtiliy(stepClone.transform, stepClone, MoveDuration, this));

            GameObject opClone = Instantiate(OpTipsContent.gameObject, OpTipsContent.parent);
            opClone.name = OpCloneKey;
            opDict.Add(OpCloneKey, new TipsContentUtiliy(opClone.transform, opClone, MoveDuration, this));

            // 初始一律隐藏，由 Controller 绑定后通过 ApplyState 决定显示状态
            HideAll();

            StepPartSwitchToggle.onValueChanged.AddListener(HandleStepToggleChanged);
            OpPartSwitchToggle.onValueChanged.AddListener(HandleOpToggleChanged);
            m_Ready = true;
        }

        protected override void OnDestroy()
        {
            if (StepPartSwitchToggle != null)
                StepPartSwitchToggle.onValueChanged.RemoveListener(HandleStepToggleChanged);
            if (OpPartSwitchToggle != null)
                OpPartSwitchToggle.onValueChanged.RemoveListener(HandleOpToggleChanged);

            base.OnDestroy();
        }
        #endregion

        #region 对外显示接口（由 Controller 调用）
        /// <summary>同步某一侧提示条的开关与显隐（不触发 OnXxxToggled，避免控制器自我回调）。</summary>
        public void SetSideState(bool isStep, bool isOpen)
        {
            if (!m_Ready) return;

            SetToggleWithoutNotify(isStep ? StepPartSwitchToggle : OpPartSwitchToggle, isOpen);
            GetSerialized(isStep).SwitchContent(isOpen);
            GetClone(isStep).SetPosState(false);
        }

        /// <summary>同步两侧开关与显隐（面板每次绑定/重建后回填 Controller 中保存的状态）。</summary>
        public void ApplyState(bool isStepOpen, bool isOpOpen)
        {
            SetSideState(true, isStepOpen);
            SetSideState(false, isOpOpen);
        }

        /// <summary>立即写入内容（不播过渡动画）：true = 步骤提示，false = 操作提示；imageKey 非空时优先显示图片。</summary>
        public void SetContentImmediate(bool isStep, string text, string imageKey = "")
        {
            if (!m_Ready) return;

            TipsContentUtiliy serialized = GetSerialized(isStep);
            if (!string.IsNullOrEmpty(imageKey))
            {
                serialized.SetContent("");
                serialized.LoadAndSetImage(imageKey, text);
            }
            else
            {
                serialized.SetContent(text ?? "");
            }
        }

        /// <summary>带双缓冲过渡动画地显示新文本内容（true = 步骤提示，false = 操作提示）。</summary>
        public void SetContent(bool isStep, string text)
        {
            SetContentWithImage(isStep, null, text);
        }

        /// <summary>带双缓冲过渡动画地显示新内容：imageKey 非空时优先显示图片，加载失败自动回退到 text。</summary>
        public void SetContentWithImage(bool isStep, string imageKey, string text)
        {
            if (!m_Ready) return;

            // 提示条压在其他面板之上
            transform.SetAsLastSibling();

            TipsContentUtiliy clone = GetClone(isStep);
            if (!string.IsNullOrEmpty(imageKey))
            {
                clone.SetContent("");
                clone.LoadAndSetImage(imageKey, text);
            }
            else
            {
                clone.SetContent(text ?? "");
            }

            AnimSwap(isStep);
        }

        /// <summary>读取某一侧当前承载的文本内容（true = 步骤提示，false = 操作提示）。</summary>
        public string GetContent(bool isStep)
        {
            return m_Ready ? GetSerialized(isStep).GetCurrentContent() : "";
        }

        /// <summary>当前对用户可见的提示文本：操作提示优先，其次步骤提示（供 AI 上下文等只读用途）。</summary>
        public string GetTipsText()
        {
            if (!m_Ready) return "";

            string op = CleanRichText(GetContent(false));
            return !string.IsNullOrEmpty(op) ? op : CleanRichText(GetContent(true));
        }
        #endregion

        #region 双缓冲动画
        // WHY: 快速连续切换时用版本号丢弃过时回调，否则旧动画会把内容/位置写回错的状态
        /// <summary>内容交换：序列化对象收起，clone 带新内容入场，结束后把内容并回序列化对象。</summary>
        void AnimSwap(bool isStep)
        {
            TipsContentUtiliy serialized = GetSerialized(isStep);
            TipsContentUtiliy clone = GetClone(isStep);

            int currentVersion = isStep ? ++stepAnimVersion : ++opAnimVersion;

            serialized.SwitchContent(false);

            clone.SwitchContent(true, () =>
            {
                int latestVersion = isStep ? stepAnimVersion : opAnimVersion;
                if (currentVersion != latestVersion) return;

                // 先停掉序列化对象可能仍在运行的收起动画，防止它把 alpha 再压回 0
                serialized.StopAnimation();

                serialized.SetContent(clone.GetCurrentContent());
                serialized.SetPosState(true);
                clone.SetPosState(false);
            });
        }

        void HideAll()
        {
            foreach (var kv in stepDict) kv.Value.SetPosState(false);
            foreach (var kv in opDict) kv.Value.SetPosState(false);
        }
        #endregion

        #region 交互
        void HandleStepToggleChanged(bool isOn)
        {
            OnStepPartToggled?.Invoke(isOn);
        }

        void HandleOpToggleChanged(bool isOn)
        {
            OnOpPartToggled?.Invoke(isOn);
        }

        void SetToggleWithoutNotify(Toggle toggle, bool isOn)
        {
            if (toggle == null) return;
            toggle.SetIsOnWithoutNotify(isOn);
        }
        #endregion

        #region 工具
        TipsContentUtiliy GetSerialized(bool isStep)
        {
            return isStep ? stepDict[StepSerializedKey] : opDict[OpSerializedKey];
        }

        TipsContentUtiliy GetClone(bool isStep)
        {
            return isStep ? stepDict[StepCloneKey] : opDict[OpCloneKey];
        }

        /// <summary>去掉 TMP 的排版标签（ChineseText 写入的 &lt;space=2em&gt;），返回可读纯文本。</summary>
        static string CleanRichText(string richText)
        {
            if (string.IsNullOrEmpty(richText)) return "";
            return richText.Replace("<space=2em>", "").Replace("<space>", "");
        }
        #endregion
    }
}
