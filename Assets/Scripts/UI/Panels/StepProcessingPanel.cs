using TMPro;

namespace MCV_Module.UI.Panels
{
    public class StepProcessingPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _processingNameText;
        [SerializeField] private TextMeshProUGUI _stepProgressText;

        public void SetProcessingName(string name)
        {
            if (_processingNameText != null)
                _processingNameText.text = name;
        }

        public void SetProgressText(string text)
        {
            if (_stepProgressText != null)
                _stepProgressText.text = text;
        }
    }
}
