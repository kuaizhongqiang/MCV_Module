
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class RenderQualityPanel : PanelBase
    {
        [SerializeField] Button _closeBtn;
        [SerializeField] ToggleGroup _toggleGroup;
        [SerializeField] TextMeshProUGUI _infoText;

        public event Action<int> OnQualityChanged;
        public event Action OnClose;

        public int QualityLevel
        {
            get
            {
                return GetToggleSelectedIndex();
            }
            set
            {
                SetToggleSelected(value);
            }
        }

        protected override void Awake()
        {
            base.Awake();

            _closeBtn.onClick.AddListener(() =>
            {
                OnClose?.Invoke();
            });

            for (int i = 0; i < _toggleGroup.transform.childCount; i++)
            {
                var toggle = _toggleGroup.transform.GetChild(i).GetComponent<Toggle>();
                toggle.onValueChanged.AddListener( (isOn) => OnToggleChanged(i));
            }
        }
        
        public void SetInfoText(string text)
        {
            if (_infoText != null)
                _infoText.text = text;
        }

        public void OnCloseBtnClick()
        {
            
        }

        void OnToggleChanged(int index)
        {
            OnQualityChanged?.Invoke(index);
        }

        bool GetToggleSelected(int index)
        {
            return _toggleGroup.transform.GetChild(index).GetComponent<Toggle>().isOn;
        }

        int GetToggleSelectedIndex()
        {
            for (int i = 0; i < _toggleGroup.transform.childCount; i++)
            {
                if (GetToggleSelected(i))
                {
                    return i;
                }
            }
            return -1;
        }

        void SetToggleSelected(int index)
        {            
            _toggleGroup.transform.GetChild(index).GetComponent<Toggle>().isOn = true;            
        }
    }
}
