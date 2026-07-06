using MCV_Module.Event;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class StartPanel : PanelBase
    {
        [SerializeField] private Button _startButton;

        private void Awake()
        {
            if (_startButton != null)
                _startButton.onClick.AddListener(() =>
                {
                    EventBus<PanelVisibilityEvent>.Publish(new PanelVisibilityEvent("StartPanel", false));
                });
        }

        public void SetStartCallback(System.Action callback)
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(() => callback?.Invoke());
            }
        }
    }
}
