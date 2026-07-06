using MCV_Module.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class StepProcessingPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _processingNameText;
        [SerializeField] private TextMeshProUGUI _stepProgressText;

        private void OnEnable()
        {
            EventBus<ProcessChangedEvent>.Subscribe(OnProcessChanged);
            EventBus<StepCompletedEvent>.Subscribe(OnStepCompleted);
        }

        private void OnDisable()
        {
            EventBus<ProcessChangedEvent>.Unsubscribe(OnProcessChanged);
            EventBus<StepCompletedEvent>.Unsubscribe(OnStepCompleted);
        }

        private void OnProcessChanged(ProcessChangedEvent evt)
        {
            if (_processingNameText != null)
                _processingNameText.text = $"工序: {evt.ProcessingId}";
        }

        private void OnStepCompleted(StepCompletedEvent evt)
        {
            if (_stepProgressText != null)
                _stepProgressText.text = $"步骤 {evt.StepId} 完成";
        }
    }
}
