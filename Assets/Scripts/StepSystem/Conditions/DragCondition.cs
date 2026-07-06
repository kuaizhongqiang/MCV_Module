using UnityEngine;

namespace MCV_Module.StepSystem
{
    public class DragCondition : ConditionBase
    {
        [SerializeField] private string _targetObjectId;
        [SerializeField] private Vector3 _targetPosition;
        [SerializeField] private float _tolerance = 0.5f;

        private bool _dragged;
        private GameObject _targetObject;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(_targetObjectId))
                _targetObject = GameObject.Find(_targetObjectId);
            else if (!string.IsNullOrEmpty(TargetId))
                _targetObject = GameObject.Find(TargetId);
        }

        public override bool IsMet() => _dragged;

        public override void ResetCondition() => _dragged = false;

        private void Update()
        {
            if (_dragged || _targetObject == null) return;

            float dist = Vector3.Distance(_targetObject.transform.position, _targetPosition);
            if (dist <= _tolerance)
            {
                _dragged = true;
                Complete();
            }
        }
    }
}
