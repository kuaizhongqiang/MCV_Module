using MCV_Module.Event;
using MCV_Module.Interactive;
using UnityEngine;

namespace MCV_Module.StepSystem
{
    /// <summary>点击条件 —— 点击指定 Interactive 物体后满足</summary>
    public class ClickCondition : ConditionBase
    {
        [SerializeField] private string _targetObjectId;
        private bool _clicked;

        public override bool IsMet() => _clicked;

        public override void ResetCondition()
        {
            _clicked = false;
        }

        private void OnEnable()
        {
            EventBus<InteractiveClickEvent>.Subscribe(OnInteractiveClick);
        }

        private void OnDisable()
        {
            EventBus<InteractiveClickEvent>.Unsubscribe(OnInteractiveClick);
        }

        private void OnInteractiveClick(InteractiveClickEvent evt)
        {
            if (evt.ObjectId == _targetObjectId)
            {
                _clicked = true;
                Complete();
            }
        }
    }
}
