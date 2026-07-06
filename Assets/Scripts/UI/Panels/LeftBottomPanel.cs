using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class LeftBottomPanel : PanelBase
    {
        [SerializeField] private Button _taskToggleButton;
        [SerializeField] private Button _roamingToggleButton;

        public void SetTaskToggleCallback(System.Action callback)
        {
            if (_taskToggleButton != null)
                _taskToggleButton.onClick.AddListener(() => callback?.Invoke());
        }

        public void SetRoamingToggleCallback(System.Action callback)
        {
            if (_roamingToggleButton != null)
                _roamingToggleButton.onClick.AddListener(() => callback?.Invoke());
        }
    }
}
