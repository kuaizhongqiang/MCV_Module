using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class FunctionPanel : PanelBase
    {
        [SerializeField] private Button _menuButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _helpButton;

        public void SetMenuCallback(System.Action callback)
        {
            if (_menuButton != null)
                _menuButton.onClick.AddListener(() => callback?.Invoke());
        }

        public void SetResetCallback(System.Action callback)
        {
            if (_resetButton != null)
                _resetButton.onClick.AddListener(() => callback?.Invoke());
        }

        public void SetHelpCallback(System.Action callback)
        {
            if (_helpButton != null)
                _helpButton.onClick.AddListener(() => callback?.Invoke());
        }
    }
}
