using UnityEngine;

namespace MCV_Module.StepSystem
{
    /// <summary>UI 条件 —— 指定 UI 交互完成后满足</summary>
    public class UICondition : ConditionBase
    {
        [SerializeField] private string _uiElementId;
        private bool _uiCompleted;

        public override bool IsMet() => _uiCompleted;

        public override void ResetCondition()
        {
            _uiCompleted = false;
        }

        /// <summary>外部调用：标记 UI 交互完成</summary>
        public void MarkUIComplete()
        {
            _uiCompleted = true;
            Complete();
        }
    }
}
