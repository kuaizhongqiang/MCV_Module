using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class RoamingBottomPanel : PanelBase
    {
        [SerializeField] private Button _forwardButton;
        [SerializeField] private Button _backwardButton;
        [SerializeField] private Button _exitRoamingButton;

        public void SetForwardCallback(System.Action callback)
        {
            if (_forwardButton != null)
                _forwardButton.onClick.AddListener(() => callback?.Invoke());
        }

        public void SetBackwardCallback(System.Action callback)
        {
            if (_backwardButton != null)
                _backwardButton.onClick.AddListener(() => callback?.Invoke());
        }

        public void SetExitRoamingCallback(System.Action callback)
        {
            if (_exitRoamingButton != null)
                _exitRoamingButton.onClick.AddListener(() => callback?.Invoke());
        }
    }
}
