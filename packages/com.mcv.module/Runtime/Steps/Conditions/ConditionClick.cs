using System;
using MCV_Module.Utils;
using System.Collections;
using MCV_Module.Event;
using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.Steps
{
    /// <summary>点击条件 —— 点击指定 InteractiveBase（targetObj）后满足。</summary>
    public class ConditionClick : ConditionBase
    {
        public override ConditionType Type => ConditionType.Click;

        protected override void OnPrepare()
        {
            if (step.TargetObj) step.TargetObj.gameObject.SetActive(false);
        }

        public override IEnumerator Waiting()
        {
            var target = step.TargetObj;
            if (target == null)
            {
                Log.Warning($"[ConditionClick] {step.name} targetObj 未赋值，跳过点击步骤");
                yield break;
            }
            target.gameObject.SetActive(true);
            step.ShowAnimationsAtFirstFrame();

            bool clicked = false;
            Action<GlobalInteractionEventData> handler = (data) =>
            {
                if (data.Type == GlobalInteractionType.Click && data.Target == target) clicked = true;
            };
            SubscribeInteraction(handler);
            yield return WaitUntilOrForceComplete(() => clicked);
            UnsubscribeInteraction();
        }

        protected override void OnCompleteHide()
        {
            if (step.TargetObj) step.TargetObj.gameObject.SetActive(false);
        }
    }
}
