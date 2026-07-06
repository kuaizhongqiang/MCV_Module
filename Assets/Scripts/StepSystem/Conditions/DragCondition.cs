using UnityEngine;

namespace MCV_Module.StepSystem
{
    /// <summary>拖拽条件 —— 将指定物体拖拽到目标位置后满足</summary>
    public class DragCondition : ConditionBase
    {
        [SerializeField] private string _targetObjectId;
        [SerializeField] private Vector3 _targetPosition;
        [SerializeField] private float _tolerance = 0.5f;

        private bool _dragged;

        public override bool IsMet() => _dragged;

        public override void ResetCondition()
        {
            _dragged = false;
        }

        private void Update()
        {
            if (_dragged) return;

            // 检测目标物体是否到达指定位置
            GameObject target = GameObject.Find(_targetObjectId);
            if (target != null)
            {
                float dist = Vector3.Distance(target.transform.position, _targetPosition);
                if (dist <= _tolerance)
                {
                    _dragged = true;
                    Complete();
                }
            }
        }
    }
}
