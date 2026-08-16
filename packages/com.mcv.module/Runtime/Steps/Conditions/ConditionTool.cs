using System;
using System.Collections;
using MCV_Module.Interfaces;
using MCV_Module.Managers;
using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.Steps
{
    /// <summary>
    /// 工具条件 —— 从工具面板选择工具（usingId）拖到 targetObj 上松开命中即完成。
    /// 通过 IStepToolPanel 契约查找面板控制器；未实现面板时告警并跳过（不阻塞流程）。
    /// </summary>
    public class ConditionTool : ConditionBase
    {
        public override ConditionType Type => ConditionType.Tool;

        IStepToolPanel ResolvePanel() =>
            GlobalControllerMgr.Instance != null
                ? GlobalControllerMgr.Instance.Find("StepToolPanelController") as IStepToolPanel
                : null;

        protected override void OnPrepare()
        {
            if (step.TargetObj) step.TargetObj.gameObject.SetActive(false);
            var panel = ResolvePanel();
            if (panel != null)
            {
                panel.SetToolDragging(null);
                panel.ClosePanel();
            }
        }

        public override IEnumerator Waiting()
        {
            step.ShowAnimationsAtFirstFrame();
            var panel = ResolvePanel();
            if (panel == null)
            {
                Debug.LogWarning($"[ConditionTool] {step.name} 未找到 StepToolPanelController（GlobalControllerMgr 未注册），跳过工具步骤");
                yield break;
            }
            if (step.TargetObj) step.TargetObj.gameObject.SetActive(true);

            bool toolPressed = false;
            Action<string> onPressed = (id) => { if (id == step.UsingId) toolPressed = true; };
            panel.OnToolPressed += onPressed;
            panel.ShowPanel();

            bool success = false;
            while (!IsForceCompleted && !success)
            {
                toolPressed = false;
                while (!IsForceCompleted && !toolPressed) yield return null;
                if (IsForceCompleted) break;
                panel.SetToolDragging(step.UsingId);
                while (!IsForceCompleted && !IsMouseUp()) yield return null;
                if (IsForceCompleted) break;
                panel.SetToolDragging(null);
                success = RaycastHitTarget(step.TargetObj);
            }

            panel.OnToolPressed -= onPressed;
            panel.ClosePanel();
        }

        protected override void OnCompleteHide()
        {
            if (step.TargetObj) step.TargetObj.gameObject.SetActive(false);
            var panel = ResolvePanel();
            if (panel != null) panel.ClosePanel();
        }
    }
}
