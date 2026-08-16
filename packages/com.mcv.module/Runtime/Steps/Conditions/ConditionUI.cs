using System;
using System.Collections;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.Steps
{
    /// <summary>
    /// UI 信息条件 —— 用 usingId(uiId) 弹出信息面板，用户关闭面板即完成。
    /// 通过 IStepUiPanel 契约查找面板控制器；未实现时告警并跳过。
    /// </summary>
    public class ConditionUI : ConditionBase
    {
        public override ConditionType Type => ConditionType.UI;

        IStepUiPanel ResolvePanel() =>
            GlobalControllerMgr.Instance != null
                ? GlobalControllerMgr.Instance.Find("StepUiPanelController") as IStepUiPanel
                : null;

        protected override void OnPrepare()
        {
            var panel = ResolvePanel();
            if (panel != null) panel.ClosePanel(); // 应对跳转回来的场景
        }

        public override IEnumerator Waiting()
        {
            step.ShowAnimationsAtFirstFrame();
            var panel = ResolvePanel();
            if (panel == null)
            {
                Debug.LogWarning($"[ConditionUI] {step.name} 未找到 StepUiPanelController，跳过 UI 步骤");
                yield break;
            }

            bool closed = false;
            Action onClosed = () => closed = true;
            panel.OnPanelClosed += onClosed;
            panel.ShowData(step.UsingId);
            yield return WaitUntilOrForceComplete(() => closed);
            panel.OnPanelClosed -= onClosed;
            panel.ClosePanel();
        }

        protected override void OnCompleteHide()
        {
            var panel = ResolvePanel();
            if (panel != null) panel.ClosePanel();
        }
    }
}
