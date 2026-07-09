using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class AiDialogPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _dialogText;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _sendButton;
        [SerializeField] private Transform _messageContainer;
        [SerializeField] private GameObject _messagePrefab;

        public event Action<string> OnSendMessage;

        protected override void Awake()
        {
            if (_sendButton != null)
                _sendButton.onClick.AddListener(SendMessage);
        }

        public void AppendMessage(string sender, string content)
        {
            if (_messageContainer == null || _messagePrefab == null) return;

            var msg = Instantiate(_messagePrefab, _messageContainer);
            var text = msg.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"<b>{sender}:</b> {content}";
        }

        public void SetDialogText(string text)
        {
            if (_dialogText != null)
                _dialogText.text = text;
        }

        private void SendMessage()
        {
            if (_inputField == null || string.IsNullOrWhiteSpace(_inputField.text)) return;

            string msg = _inputField.text;
            AppendMessage("用户", msg);
            OnSendMessage?.Invoke(msg);
            _inputField.text = "";
        }

        /// <summary>添加 AI 回复</summary>
        public void AddAiResponse(string response)
        {
            AppendMessage("AI", response);
        }
    }
}
