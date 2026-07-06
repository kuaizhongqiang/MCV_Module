using TMPro;

namespace MCV_Module.UI.Panels
{
    public class RightUpPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _infoText;

        public void SetInfo(string text)
        {
            if (_infoText != null)
                _infoText.text = text;
        }
    }
}
