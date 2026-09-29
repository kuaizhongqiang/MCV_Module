using System.Collections;
using MCV_Module.Event;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.Project;
using MCV_Module.Steps;
using MCV_Module.UI.Panels;
using MCV_Module.Utils;
using UnityEngine;
using MCV_Module.Utils;

namespace MCV_Module.Controllers
{
    // WHY: 面板随 Canvas 重建、只做显示，开关状态 / 自动收起计时 / 是否自动收起的业务判断都必须留在 Controller，重绑时回填
    /// <summary>提示条控制器：订阅 StepPreparedEvent，把步骤数据译成「步骤提示 / 操作提示」推给面板，并管理开关展开与自动收起计时。</summary>
    public class TipsController : ControllerBase<TipsPanel>
    {
        // ── 显示状态（跨面板重建保留）──
        bool m_StepPartOpen = true;
        bool m_OpPartOpen;              // 操作提示默认收起，由用户主动展开

        // WHY: 要区分「被自动收起」与「用户手动收起」—— 混淆会让第一步提示自动收起后，后续步骤只静默更新、学生看不到提示
        /// <summary>该侧是被自动收起计时关掉的（区别于用户手动收起），新内容到来时应重新展开。</summary>
        bool m_StepAutoHidden;
        bool m_OpAutoHidden;

        // ── 当前内容缓存（面板重建后回填用）──
        bool m_HasContent;
        string m_StepText = "";
        string m_StepImageKey = "";
        string m_OpText = "";
        string m_OpImageKey = "";

        Coroutine m_StepTimer;
        Coroutine m_OpTimer;

        #region 生命周期
        public override void OnInit()
        {
            base.OnInit();
            // Controller 常驻 → 一次订阅即可（EventBus 内部去重）
            EventBus<StepPreparedEvent>.Subscribe(OnStepPrepared);
        }

        public override void OnDispose()
        {
            EventBus<StepPreparedEvent>.Unsubscribe(OnStepPrepared);
            StopTimer(true);
            StopTimer(false);
            UnbindView();
            base.OnDispose();
        }

        public override void OnViewBound()
        {
            // 先清后加，避免面板重建后重复订阅
            View.OnStepPartToggled -= OnStepPartToggled;
            View.OnStepPartToggled += OnStepPartToggled;
            View.OnOpPartToggled -= OnOpPartToggled;
            View.OnOpPartToggled += OnOpPartToggled;

            // 回填上次内容（静默写入，不播过渡动画）与开关状态
            if (m_HasContent)
            {
                View.SetContentImmediate(true, m_StepText, m_StepImageKey);
                View.SetContentImmediate(false, m_OpText, m_OpImageKey);
            }
            View.ApplyState(m_StepPartOpen, m_OpPartOpen);

            if (m_StepPartOpen) StartTimer(true);
            if (m_OpPartOpen) StartTimer(false);
        }

        void UnbindView()
        {
            if (View == null) return;
            View.OnStepPartToggled -= OnStepPartToggled;
            View.OnOpPartToggled -= OnOpPartToggled;
        }
        #endregion

        #region 步骤 → 提示内容
        // WHY: 提示优先取 StepContentData 里 tipsId 指向的条目（两侧都给），没配才回退到步骤 description（此时操作提示清空）
        /// <summary>步骤进入等待用户操作：把步骤数据翻译成提示内容推给面板。</summary>
        void OnStepPrepared(StepPreparedEvent e)
        {
            if (e == null || e.Step == null) return;

            StepTipsData tips = FindTips(e.Step.TipsId);
            if (tips != null)
            {
                SetStepTips(Localized.Pick(tips.stepTips, tips.stepTipsEn), tips.stepImageKey);
                SetOpTips(Localized.Pick(tips.opTips, tips.opTipsEn), tips.opImageKey);
                return;
            }

            SetStepTips(e.Step.Description);
            SetOpTips(null);
        }

        /// <summary>按 tipsId 从 StepContentData 取 StepTipsData 条目（容器多态，只挑 StepTipsData；取不到返回 null）。</summary>
        static StepTipsData FindTips(string tipsId)
        {
            if (string.IsNullOrEmpty(tipsId)) return null;
            if (!GlobalDataMgr.Exists || GlobalDataMgr.Instance == null) return null;

            var data = GlobalDataMgr.Instance.StepContentData;
            if (data == null || data.contents == null) return null;

            for (int i = 0; i < data.contents.Count; i++)
            {
                if (data.contents[i] is StepTipsData tips && tips.id == tipsId) return tips;
            }
            return null;
        }

        #region 对外填写接口
        /// <summary>填写【步骤提示】（面板 StepTipsPart 下的 text；面板不存在时只更新缓存，重建后自动回填）。</summary>
        public void SetStepTips(string text, string imageKey = null)
        {
            ApplySide(true, text, imageKey);
        }

        /// <summary>填写【操作提示】（面板 OperationTipsPart 下的 text），口径同 <see cref="SetStepTips"/>。</summary>
        public void SetOpTips(string text, string imageKey = null)
        {
            ApplySide(false, text, imageKey);
        }

        /// <summary>一次填写两侧提示（步骤提示 + 操作提示）。</summary>
        public void SetTips(string stepText, string opText)
        {
            SetStepTips(stepText);
            SetOpTips(opText);
        }

        void ApplySide(bool isStep, string text, string imageKey)
        {
            if (isStep)
            {
                m_StepText = text ?? "";
                m_StepImageKey = imageKey ?? "";
            }
            else
            {
                m_OpText = text ?? "";
                m_OpImageKey = imageKey ?? "";
            }

            m_HasContent = HasAnyContent();

            if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(imageKey))
            {
                // 清空：只清文本，不动开关状态
                if (View != null) View.SetContentImmediate(isStep, "", "");
                return;
            }

            // 新内容 → 若这一侧是被"自动收起"关掉的，重新展开（并重新计时）；用户手动收起的则尊重其选择
            ReopenIfAutoHidden(isStep);

            PushToView(isStep);
        }

        /// <summary>这一侧处于"自动收起"状态时重新展开并重启自动收起计时（仅当 AutoHidden 为真，即不是用户收起的）。</summary>
        void ReopenIfAutoHidden(bool isStep)
        {
            if (isStep)
            {
                if (!m_StepAutoHidden) return;
                m_StepAutoHidden = false;
                m_StepPartOpen = true;
            }
            else
            {
                if (!m_OpAutoHidden) return;
                m_OpAutoHidden = false;
                m_OpPartOpen = true;
            }

            if (View != null) View.SetSideState(isStep, true);
            StartTimer(isStep);
        }

        bool HasAnyContent()
        {
            return !string.IsNullOrEmpty(m_StepText) || !string.IsNullOrEmpty(m_StepImageKey)
                || !string.IsNullOrEmpty(m_OpText) || !string.IsNullOrEmpty(m_OpImageKey);
        }
        #endregion

        /// <summary>推内容到面板：展开时播过渡动画，收起时静默更新（下次展开自然带上）。</summary>
        void PushToView(bool isStep)
        {
            if (View == null) return;

            bool isOpen = isStep ? m_StepPartOpen : m_OpPartOpen;
            string text = isStep ? m_StepText : m_OpText;
            string imageKey = isStep ? m_StepImageKey : m_OpImageKey;

            if (isOpen) View.SetContentWithImage(isStep, imageKey, text);
            else View.SetContentImmediate(isStep, text, imageKey);
        }
        #endregion

        #region 开关交互
        void OnStepPartToggled(bool isOpen)
        {
            m_StepPartOpen = isOpen;
            m_StepAutoHidden = false;       // 用户的显式选择，覆盖"自动收起"标记
            if (View != null) View.SetSideState(true, isOpen);

            if (isOpen) StartTimer(true);
            else StopTimer(true);
        }

        void OnOpPartToggled(bool isOpen)
        {
            m_OpPartOpen = isOpen;
            m_OpAutoHidden = false;
            if (View != null) View.SetSideState(false, isOpen);

            if (isOpen) StartTimer(false);
            else StopTimer(false);
        }
        #endregion

        #region 自动收起计时
        /// <summary>启动某一侧的自动收起计时（时长取面板配置，&lt;=0 不收起；连线 / 仿真任务不自动收起）。</summary>
        void StartTimer(bool isStep)
        {
            StopTimer(isStep);

            float delay = View != null ? View.AutoHideDelay : 0f;
            if (delay <= 0f || !IsAutoHideAllowed()) return;

            Coroutine routine = Run(AutoHideAfter(delay, isStep));
            if (isStep) m_StepTimer = routine;
            else m_OpTimer = routine;
        }

        void StopTimer(bool isStep)
        {
            if (isStep)
            {
                if (m_StepTimer != null) Halt(m_StepTimer);
                m_StepTimer = null;
            }
            else
            {
                if (m_OpTimer != null) Halt(m_OpTimer);
                m_OpTimer = null;
            }
        }

        /// <summary>自动收起是否适用当前任务（连线/仿真任务提示常驻）。</summary>
        bool IsAutoHideAllowed()
        {
            TaskType taskType = GetCurrentTaskType();
            return taskType != TaskType.LineConnection && taskType != TaskType.Training;
        }

        TaskType GetCurrentTaskType()
        {
            // 读唯一源（由 TaskTypeChangeEventData 写入，见 GlobalUIMgr.OnTaskTypeChanged）
            return GlobalDataMgr.GetCurrentTaskType();
        }

        IEnumerator AutoHideAfter(float delay, bool isStep)
        {
            yield return new WaitForSeconds(delay);

            if (isStep)
            {
                m_StepTimer = null;
                if (!m_StepPartOpen) yield break; // 已被用户手动展开/收起，跳过
                m_StepPartOpen = false;
                m_StepAutoHidden = true;          // 记成"自动收起"：下一步的提示到来时要重新展开
            }
            else
            {
                m_OpTimer = null;
                if (!m_OpPartOpen) yield break;
                m_OpPartOpen = false;
                m_OpAutoHidden = true;
            }

            if (View != null) View.SetSideState(isStep, false);
        }
        #endregion
    }
}
