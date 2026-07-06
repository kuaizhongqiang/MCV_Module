using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MCV_Module.UI.Panels
{
    public class MenuPanel : PanelBase
    {
        [SerializeField] private Transform _buttonContainer;
        [SerializeField] private GameObject _buttonPrefab;

        public void CreateExperimentButtons(string[] experimentNames, System.Action<int> onClick)
        {
            if (_buttonContainer == null || _buttonPrefab == null) return;

            for (int i = _buttonContainer.childCount - 1; i >= 0; i--)
                Destroy(_buttonContainer.GetChild(i).gameObject);

            for (int i = 0; i < experimentNames.Length; i++)
            {
                var index = i;
                var btnObj = Instantiate(_buttonPrefab, _buttonContainer);
                var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                    btnText.text = experimentNames[i];

                var btn = btnObj.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => onClick?.Invoke(index));
            }
        }
    }
}
