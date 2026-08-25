using System.Collections;
using MCV_Module.Utils;
using System.Collections.Generic;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Objects.Interactives.Elements;
using UnityEngine;

namespace MCV_Module.Steps
{
    /// <summary>
    /// 连线条件 —— 复用常驻连线系统（ElementManagerBase 状态机）。
    /// step.Lines 存"目标连线模板"（ElementLineObj，inactive，PointList 预填两端点）。
    /// 轮询 ElementManagerBase 已连接的线，所有模板端点对都有匹配即完成（顺序无关）。
    /// 跳转离开时 CancelDrawing 取消临时线，不销毁已提交的常驻线（跳回后仍算完成，保证状态延续）。
    /// </summary>
    public class ConditionLineConnect : ConditionBase
    {
        public override ConditionType Type => ConditionType.LineConnect;

        protected override void OnPrepare()
        {
            if (ElementManagerBase.Instance != null) ElementManagerBase.Instance.CancelDrawing();
            HideLineElements();
        }

        public override IEnumerator Waiting()
        {
            var mgr = ElementManagerBase.Instance;
            if (mgr == null)
            {
                Log.Warning($"[ConditionLineConnect] {step.name} 无 ElementManagerBase，跳过连线步骤");
                yield break;
            }
            ShowLineElements();
            step.ShowAnimationsAtFirstFrame();
            while (!IsForceCompleted && !AllLinesConnected(mgr))
                yield return null;
            HideLineElements(); // 完成后隐藏模板；已连接的常驻线保留
        }

        protected override void OnCompleteHide() => HideLineElements();

        /// <summary>快速完成：快速显示所有线模板（跳转快进时呈现"已连完"的视觉）再走基类</summary>
        protected override IEnumerator FastComplete()
        {
            foreach (var line in step.Lines)
                if (line != null) line.gameObject.SetActive(true);
            yield return base.FastComplete();
        }

        /// <summary>所有模板端点对都有已连接线匹配（顺序无关）</summary>
        bool AllLinesConnected(ElementManagerBase mgr)
        {
            foreach (var line in step.Lines)
            {
                var tpl = line as ElementLineObj;
                if (tpl == null || tpl.PointList.Count < 2) continue;
                var a = tpl.PointList[0];
                var b = tpl.PointList[tpl.PointList.Count - 1];
                bool matched = false;
                foreach (var live in mgr.GetLines())
                {
                    if (live.Matches(a, b)) { matched = true; break; }
                }
                if (!matched) return false;
            }
            return true;
        }

        void ShowLineElements() => SetLineElementsActive(true);
        void HideLineElements() => SetLineElementsActive(false);

        /// <summary>模板涉及的 Element（去重父级）显隐</summary>
        void SetLineElementsActive(bool active)
        {
            var parents = new HashSet<ElementObjBase>();
            foreach (var line in step.Lines)
            {
                var tpl = line as ElementLineObj;
                if (tpl == null) continue;
                foreach (var p in tpl.PointList)
                {
                    if (p == null) continue;
                    var elem = p.GetComponentInParent<ElementObjBase>();
                    if (elem != null) parents.Add(elem);
                }
            }
            foreach (var e in parents)
                if (e != null) e.gameObject.SetActive(active);
        }
    }
}
