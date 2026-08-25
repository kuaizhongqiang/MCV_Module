using System;
using MCV_Module.Utils;
using System.Collections;
using MCV_Module.Event;
using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.Steps
{
    /// <summary>
    /// 拖拽条件 —— 将 dragObj 拖到 targetObj 上松开命中即完成，未命中恢复可重试。
    /// 按下检测走全局交互 Down 事件（命中 dragObj）；松开检测直接查左键状态
    /// （GlobalInteractiveMgr 在空白处松开只发 Target=null 的 Click，无 Up 事件）。
    /// </summary>
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
            while (!IsForceCompleted && !success)
            {
                // 等待按下 dragObj 起拖
                while (!IsForceCompleted && !dragDown) yield return null;
                if (IsForceCompleted) break;
                dragDown = false;
                drag.gameObject.SetActive(false); // 拿起（隐藏源物体）
                // 等待松开
                while (!IsForceCompleted && !IsMouseUp()) yield return null;
                if (IsForceCompleted) break;
                // 松开时手动射线命中 targetObj → 成功；否则恢复重来
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
