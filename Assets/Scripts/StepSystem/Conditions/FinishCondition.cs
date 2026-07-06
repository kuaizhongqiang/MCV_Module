using MCV_Module.Event;
using UnityEngine;

namespace MCV_Module.StepSystem
{
    /// <summary>完成条件 —— 标记整个流程结束</summary>
    public class FinishCondition : ConditionBase
    {
        private bool _finished;

        public override bool IsMet() => _finished;

        public override void ResetCondition()
        {
            _finished = false;
        }

        private void Start()
        {
            // Finish 条件自动触发
            _finished = true;
            Complete();
        }
    }
}
