using System;
using MCV_Module.GlobalManager;
using MCV_Module.UI.Canvas;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class DialogPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action _onConfirm;
        private Action _onCancel;

        protected override void Awake()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(() =>
                {
                    _onConfirm?.Invoke();
                    SetUIActive(false);
                });

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(() =>
                {
                    _onCancel?.Invoke();
                    SetUIActive(false);
                });
        }

        public void Show(string title, string message, Action onConfirm = null, Action onCancel = null)
        {
            if (_titleText != null) _titleText.text = title;
            if (_messageText != null) _messageText.text = message;
            // 需要进行Layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(GlobalUiMgr.GetCanvas<FunctionCanvas>().GetComponent<RectTransform>());
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            SetUIActive(true);
        }
    }
}
