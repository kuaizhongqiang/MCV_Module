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

        private System.Func<string, bool> _onSubmit;

        private void Awake()
        {
            if (_submitButton != null)
                _submitButton.onClick.AddListener(OnSubmitClicked);
        }

        public void ShowQuestion(string question, System.Func<string, bool> onSubmit)
        {
            if (_questionText != null) _questionText.text = question;
            _onSubmit = onSubmit;
            if (_answerInput != null) _answerInput.text = "";
            if (_resultText != null) _resultText.text = "";
            SetUIActive(true);
        }

        private void OnSubmitClicked()
        {
            if (_onSubmit == null) return;
            string answer = _answerInput != null ? _answerInput.text : "";
            bool correct = _onSubmit(answer);

            if (_resultText != null)
                _resultText.text = correct ? "✓ 回答正确！" : "✗ 回答错误，请重试";
        }
    }
}
