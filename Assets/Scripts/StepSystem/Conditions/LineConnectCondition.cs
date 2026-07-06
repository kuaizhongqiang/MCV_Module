using MCV_Module.Event;
using UnityEngine;

namespace MCV_Module.StepSystem
{
    /// <summary>连线条件 —— 指定线组连接完成后满足</summary>
    public class LineConnectCondition : ConditionBase
    {
        [SerializeField] private string _targetGroupId;
        private bool _connected;

        public override bool IsMet() => _connected;

        public override void ResetCondition()
        {
            _connected = false;
        }

        private void OnEnable()
        {
            EventBus<LineConnectedEvent>.Subscribe(OnLineConnected);
        }

        private void OnDisable()
        {
            EventBus<LineConnectedEvent>.Unsubscribe(OnLineConnected);
        }

        private void OnLineConnected(LineConnectedEvent evt)
        {
            if (evt.GroupId == _targetGroupId && evt.IsConnected)
            {
                _connected = true;
                Complete();
            }
        }
    }
}
