using MCV_Module.Data.Project;
using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class TaskTestPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _questionText;

        public void ShowQuestion(TaskTestData data)
        {
            if (data?.QuestionClip == null || data.QuestionClip.questions.Count == 0) return;
            if (_questionText != null)
                _questionText.text = data.QuestionClip.questions[0].questionText ?? "（题目待配置）";
        }
    }
}
