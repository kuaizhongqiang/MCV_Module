using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class StepToolPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _toolNameText;
        [SerializeField] private TextMeshProUGUI _toolDescriptionText;
        [SerializeField] private Transform _toolButtonContainer;
        [SerializeField] private GameObject _toolButtonPrefab;

        public event Action<string> OnToolSelected;

        public void ShowTool(string name, string description)
        {
            if (_toolNameText != null) _toolNameText.text = name;
            if (_toolDescriptionText != null) _toolDescriptionText.text = description;
            SetUIActive(true);
        }

        public void ShowTools(string[] toolNames)
        {
            if (_toolButtonContainer == null || _toolButtonPrefab == null) return;

            for (int i = _toolButtonContainer.childCount - 1; i >= 0; i--)
                Destroy(_toolButtonContainer.GetChild(i).gameObject);

            foreach (var toolName in toolNames)
            {
                var btn = Instantiate(_toolButtonPrefab, _toolButtonContainer);
                var text = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = toolName;

                var captureName = toolName;
                btn.GetComponent<Button>()?.onClick.AddListener(() => OnToolSelected?.Invoke(captureName));
            }

            SetUIActive(true);
        }

        public void HideTool() => SetUIActive(false);
    }
}
