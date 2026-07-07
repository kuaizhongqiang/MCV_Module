using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class StepQuestionPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _questionText;
        [SerializeField] private TMP_InputField _answerInput;
        [SerializeField] private Button _submitButton;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private int _maxAttempts = 3;

        private Func<string, bool> _onSubmit;
        private int _attemptCount;

        protected override void Awake()
        {
            if (_submitButton != null)
                _submitButton.onClick.AddListener(OnSubmitClicked);
        }

        public void ShowQuestion(string question, Func<string, bool> onSubmit)
        {
            if (_questionText != null) _questionText.text = question;
            _onSubmit = onSubmit;
            _attemptCount = 0;
            if (_answerInput != null) _answerInput.text = "";
            if (_resultText != null) _resultText.text = "";
            if (_submitButton != null) _submitButton.interactable = true;
            SetUIActive(true);
        }

        private void OnSubmitClicked()
        {
            if (_onSubmit == null) return;

            _attemptCount++;
            string answer = _answerInput != null ? _answerInput.text : "";
            bool correct = _onSubmit(answer);

            if (_resultText != null)
            {
                if (correct)
                    _resultText.text = "✓ 回答正确！";
                else if (_attemptCount >= _maxAttempts)
                {
                    _resultText.text = $"✗ 已达最大尝试次数 ({_maxAttempts})";
                    if (_submitButton != null) _submitButton.interactable = false;
                }
                else
                    _resultText.text = $"✗ 回答错误，剩余 {_maxAttempts - _attemptCount} 次机会";
            }
        }
    }
}
