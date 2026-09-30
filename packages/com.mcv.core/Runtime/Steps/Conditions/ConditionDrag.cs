using System;
using MCV_Module.Utils;
using System.Collections;
using MCV_Module.Event;
using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.Steps
{
    /// <summary>拖拽条件：把 dragObj 拖到 targetObj 上松开命中即完成，未命中可重试。</summary>
    public class ConditionDrag : ConditionBase
    {
        public override ConditionType Type => ConditionType.Drag;

        protected override void OnPrepare()
        {
            if (step.DragObj) step.DragObj.gameObject.SetActive(false);
            if (step.TargetObj) step.TargetObj.gameObject.SetActive(false);
        }

        public override IEnumerator Waiting()
        {
            var drag = step.DragObj;
            var target = step.TargetObj;
            if (drag == null || target == null)
            {
                Log.Warning($"[ConditionDrag] {step.name} dragObj/targetObj 未赋值，跳过拖拽步骤");
                yield break;
            }
            drag.gameObject.SetActive(true);
            target.gameObject.SetActive(true);
            step.ShowAnimationsAtFirstFrame();

            bool dragDown = false;
            Action<GlobalInteractionEventData> handler = (data) =>
            {
                if (data.Type == GlobalInteractionType.Down && data.Target == drag) dragDown = true;
            };
            SubscribeInteraction(handler);

            bool success = false;
            // 拿起 → 等松开 → 射线命中 targetObj 才算成功，未命中恢复重来（详见 ConditionDrag.md）
            while (!IsForceCompleted && !success)
            {
                while (!IsForceCompleted && !dragDown) yield return null;
                if (IsForceCompleted) break;
                dragDown = false;
                drag.gameObject.SetActive(false);
                while (!IsForceCompleted && !IsMouseUp()) yield return null;
                if (IsForceCompleted) break;
                if (RaycastHitTarget(target)) success = true;
                else drag.gameObject.SetActive(true);
            }
            UnsubscribeInteraction();
        }

        protected override void OnCompleteHide()
        {
            if (step.DragObj) step.DragObj.gameObject.SetActive(false);
            if (step.TargetObj) step.TargetObj.gameObject.SetActive(false);
        }
    }
}
