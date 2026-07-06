using MCV_Module.Event;
using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class StepPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _stepNameText;
        [SerializeField] private TextMeshProUGUI _stepDescriptionText;
        [SerializeField] private TextMeshProUGUI _stepStatusText;

        private void OnEnable()
        {
            EventBus<StepPreparedEvent>.Subscribe(OnStepPrepared);
            EventBus<StepWaitingEvent>.Subscribe(OnStepWaiting);
            EventBus<StepCompletedEvent>.Subscribe(OnStepCompleted);
        }

        private void OnDisable()
        {
            EventBus<StepPreparedEvent>.Unsubscribe(OnStepPrepared);
            EventBus<StepWaitingEvent>.Unsubscribe(OnStepWaiting);
            EventBus<StepCompletedEvent>.Unsubscribe(OnStepCompleted);
        }

        public void SetStepInfo(string name, string description)
        {
            if (_stepNameText != null) _stepNameText.text = name;
            if (_stepDescriptionText != null) _stepDescriptionText.text = description;
        }

        private void OnStepPrepared(StepPreparedEvent evt)
        {
            SetStatus("准备中...");
        }

        private void OnStepWaiting(StepWaitingEvent evt)
        {
            SetStatus("等待操作...");
        }

        private void OnStepCompleted(StepCompletedEvent evt)
        {
            SetStatus("✓ 完成");
        }

        private void SetStatus(string status)
        {
            if (_stepStatusText != null)
                _stepStatusText.text = status;
        }
    }
}
