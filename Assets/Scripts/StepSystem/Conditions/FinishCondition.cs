using UnityEngine;

namespace MCV_Module.StepSystem
{
    public class FinishCondition : ConditionBase
    {
        private bool _finished;

        public override bool IsMet() => _finished;

        public override void ResetCondition()
        {
            _finished = false;
            // OnEnable 会在 ResetCondition 后由重激活触发
        }

        private void OnEnable()
        {
            _finished = true;
            Complete();
        }
    }
}
