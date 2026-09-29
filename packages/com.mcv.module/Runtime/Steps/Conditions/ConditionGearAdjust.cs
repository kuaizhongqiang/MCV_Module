using System.Collections;
using MCV_Module.Models;
using MCV_Module.Objects.Interactives.TaskObj;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Steps
{
    /// <summary>调整表旋钮：把旋钮（step.TargetObj）转到 step.GearType 的功能档位即完成（不判量程）。</summary>
    public class ConditionGearAdjust : ConditionBase
    {
        public override ConditionType Type => ConditionType.GearAdjust;

        /// <summary>目标旋钮（由 step.TargetObj 解析）</summary>
        InspectionMultimeterKnobObj knob;
        /// <summary>旋钮身上的碰撞体（逐步骤开关交互用；数量固定，解析时缓存一次）</summary>
        Collider[] knobColliders;

        /// <summary>归位：收掉旋钮交互（也是"初始化即关"的落点，ConditionInit 会调到这里）。</summary>
        public override void ResetCondition()
        {
            base.ResetCondition();
            ResolveKnob();
            SetKnobInteractable(false);
        }

        protected override void OnPrepare() => SetKnobInteractable(false);

        public override IEnumerator Waiting()
        {
            step.ShowAnimationsAtFirstFrame();

            if (!ResolveKnob())
            {
                Log.Warning($"[ConditionGearAdjust] {step.name} targetObj 不是 InspectionMultimeterKnobObj（或未赋值），跳过旋钮步骤");
                yield break;
            }

            SetKnobInteractable(true);
            yield return WaitUntilOrForceComplete(IsOnTargetGear);
            SetKnobInteractable(false);
        }

        protected override void OnCompleteHide() => SetKnobInteractable(false);

        #region 判定

        /// <summary>当前档位功能 == 步骤要求的功能档位即完成（不判量程；档位表为空时不算满足）。</summary>
        bool IsOnTargetGear()
        {
            return knob != null && knob.GearCount > 0 && knob.CurrentGearType == step.GearType;
        }

        #endregion

        #region 交互开关

        /// <summary>把 <c>step.TargetObj</c> 解析成旋钮（已解析过就直接复用；解析时缓存碰撞体）。</summary>
        bool ResolveKnob()
        {
            if (knob != null) return true;
            if (step == null) return false;

            knob = step.TargetObj as InspectionMultimeterKnobObj;
            if (knob == null) return false;

            knobColliders = knob.GetComponents<Collider>();
            return true;
        }

        // WHY: 只能切碰撞体，不能改 IsInteractable —— InteractiveBase.Awake 仅在 isInteractable 为真时自订阅 Mo*，Awake 顺序不保证，置 false 会让旋钮永远收不到 MoDown（转不动）
        /// <summary>开关旋钮交互：一个物体可能挂多个碰撞体，全部一起切。</summary>
        void SetKnobInteractable(bool on)
        {
            if (knob == null) return;

            if (knobColliders == null) knobColliders = knob.GetComponents<Collider>();

            for (int i = 0; i < knobColliders.Length; i++)
                if (knobColliders[i] != null) knobColliders[i].enabled = on;

            if (on && knobColliders.Length == 0)
                Log.Warning($"[ConditionGearAdjust] {knob.name} 上没有 Collider，旋钮收不到拖拽（无法调整）");
        }

        #endregion
    }
}
