using UnityEngine;

namespace MCV_Module.StepSystem
{
    /// <summary>工具条件 —— 使用指定工具后满足</summary>
    public class ToolCondition : ConditionBase
    {
        [SerializeField] private string _requiredToolId;
        private bool _toolUsed;

        public override bool IsMet() => _toolUsed;

        public override void ResetCondition()
        {
            _toolUsed = false;
        }

        /// <summary>外部调用：标记工具已使用</summary>
        public void MarkToolUsed()
        {
            _toolUsed = true;
            Complete();
        }
    }
}
