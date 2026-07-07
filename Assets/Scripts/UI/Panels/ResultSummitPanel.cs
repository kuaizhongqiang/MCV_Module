using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class ResultSummitPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _detailText;
        [SerializeField] private Button _confirmButton;

        public event Action OnConfirm;

        protected override void Awake()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(() =>
                {
                    OnConfirm?.Invoke();
                    SetUIActive(false);
                });
        }

        public void ShowResult(string title, float score, string detail)
        {
            if (_titleText != null) _titleText.text = title;
            if (_scoreText != null) _scoreText.text = $"得分: {score:F1}";
            if (_detailText != null) _detailText.text = detail;
            SetUIActive(true);
        }
    }
}
