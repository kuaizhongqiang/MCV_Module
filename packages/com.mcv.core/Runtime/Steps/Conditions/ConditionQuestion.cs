using System;
using MCV_Module.Utils;
using System.Collections;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.Steps
{
    /// <summary>答题条件：用 usingId（题目 id）弹答题面板，答对即完成。</summary>
    public class ConditionQuestion : ConditionBase
    {
        public override ConditionType Type => ConditionType.Question;

        IStepQuestionPanel ResolvePanel() =>
            GlobalControllerMgr.Instance != null
                ? GlobalControllerMgr.Instance.Find("StepQuestionController") as IStepQuestionPanel
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
                Log.Warning($"[ConditionQuestion] {step.name} 未找到 StepQuestionPanelController，跳过答题步骤");
                yield break;
            }

            bool correct = false;
            Action onCorrect = () => correct = true;
            panel.OnQuestionCorrect += onCorrect;
            panel.ShowQuestion(step.UsingId);
            yield return WaitUntilOrForceComplete(() => correct);
            panel.OnQuestionCorrect -= onCorrect;
            panel.ClosePanel();
        }

        protected override void OnCompleteHide()
        {
            var panel = ResolvePanel();
            if (panel != null) panel.ClosePanel();
        }
    }
}
