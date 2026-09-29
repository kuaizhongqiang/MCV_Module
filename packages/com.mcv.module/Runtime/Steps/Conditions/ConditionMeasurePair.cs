using System;
using System.Collections;
using MCV_Module.Event;
using MCV_Module.Models;
using MCV_Module.Objects.Interactives.TaskObj;
using MCV_Module.Utils;
using UnityEngine;

namespace MCV_Module.Steps
{
    /// <summary>测量一对点：红黑表笔分别吸附到 step.Points 的两个检测点即完成，红黑对调算同一对。</summary>
    public class ConditionMeasurePair : ConditionBase
    {
        public override ConditionType Type => ConditionType.MeasurePair;

        /// <summary>红表笔当前吸附的点（null = 未接入）</summary>
        InspectionElementPointObj redPoint;
        /// <summary>黑表笔当前吸附的点（null = 未接入）</summary>
        InspectionElementPointObj blackPoint;

        /// <summary>吸附事件缓存（退订用）</summary>
        Action<InspectionProbeSnapEventData> snapHandler;

        public override void ResetCondition()
        {
            base.ResetCondition();
            UnsubscribeSnap();
            redPoint = null;
            blackPoint = null;
        }

        public override IEnumerator Waiting()
        {
            step.ShowAnimationsAtFirstFrame();

            if (!TryResolvePoints(out var pointA, out var pointB))
            {
                Log.Warning($"[ConditionMeasurePair] {step.name} points 未配置成 2 个不同的检测点，跳过测量步骤");
                yield break;
            }

            redPoint = null;
            blackPoint = null;
            snapHandler = OnProbeSnapped;
            EventBus<InspectionProbeSnapEventData>.Subscribe(snapHandler);

            // WHY: 必须补读 —— 上一步已把表笔放好时不会有吸附事件，只等事件会死等
            SeedFromScene();
            yield return WaitUntilOrForceComplete(() => IsPairMatched(pointA, pointB));

            UnsubscribeSnap();
        }

        #region 表笔吸附

        /// <summary>吸附态翻转 → 更新"这一侧接在哪个点上"（脱离吸附一律记为未接入）。</summary>
        void OnProbeSnapped(InspectionProbeSnapEventData e)
        {
            if (e == null) return;
            ApplyProbe(e.ProbeType, e.IsSnapped ? e.Point : null);
        }

        /// <summary>把某一支表笔的吸附点记进本地状态。</summary>
        void ApplyProbe(InspectionProbeType probeType, InspectionElementPointObj point)
        {
            if (probeType == InspectionProbeType.Red) redPoint = point;
            else blackPoint = point;
        }

        /// <summary>补读场景里表笔的当前吸附态（表笔只有红黑两支，直接遍历，不必缓存）。</summary>
        void SeedFromScene()
        {
            var probes = UnityEngine.Object.FindObjectsByType<InspectionProbeObj>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < probes.Length; i++)
            {
                var probe = probes[i];
                if (probe == null) continue;
                ApplyProbe(probe.ProbeType, probe.SnappedPoint);
            }
        }

        void UnsubscribeSnap()
        {
            if (snapHandler == null) return;

            EventBus<InspectionProbeSnapEventData>.Unsubscribe(snapHandler);
            snapHandler = null;
        }

        #endregion

        #region 判定

        /// <summary>红黑两支表笔分别落在配置的两个点上（顺序无关）即完成。</summary>
        bool IsPairMatched(InspectionElementPointObj a, InspectionElementPointObj b)
        {
            if (redPoint == null || blackPoint == null) return false;

            return (redPoint == a && blackPoint == b) || (redPoint == b && blackPoint == a);
        }

        /// <summary>取两个检测点：必须恰好 2 个、都是检测点、且不同；取不到返回 false（调用方告警跳过）。</summary>
        bool TryResolvePoints(out InspectionElementPointObj a, out InspectionElementPointObj b)
        {
            a = null;
            b = null;

            var points = step.Points;
            if (points == null || points.Count != 2) return false;

            a = points[0] as InspectionElementPointObj;
            b = points[1] as InspectionElementPointObj;
            return a != null && b != null && a != b;
        }

        #endregion
    }
}
