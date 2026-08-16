using System.Collections;
using MCV_Module.Models;

namespace MCV_Module.Steps
{
    /// <summary>默认条件 —— 无交互，显示动画第一帧后立即完成（过渡/自动演示/纯显隐）。</summary>
    public class ConditionDefault : ConditionBase
    {
        public override ConditionType Type => ConditionType.Default;

        public override IEnumerator Waiting()
        {
            step.ShowAnimationsAtFirstFrame();
            yield break;
        }
    }
}
