using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MCV_Module.Models.Project;

namespace MCV_Module.UI.Panels
{
    /// <summary>小测验任务面板（占位实现，模仿 TaskPurposePanel）。</summary>
    public class TaskTestPanel : TaskPanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Transform questionParent;
        QuestionData currentQuestion;

        readonly QuestionClip questionList = new QuestionClip();

        public void Init(QuestionClip question)
        {
            if (question == null) return;
        }

        public void SetQuestion(QuestionClip question)
        {
            Init(question);
        }

        public void SelectQuestion(QuestionData data)
        {
            currentQuestion = data;
        }

        public override string GetPanelContent()
        {
            string result = "";
            result += "【小测验页面】\n";
            int questionCount = questionList.questions.Count;
            result += $"当前任务包含 {questionCount} 个小测验。\n";
            result += $"当前问题是：\n";
            result += $"{currentQuestion.questionText}\n";
            string options = "";
            for (int i = 0; i < currentQuestion.options.Count; i++)
            {
                var item = currentQuestion.options[i];
                options += $"{item.itemText}\n";
            }
            result += $"选项是：\n";
            result += $"{options}\n";
            string answer = "";
            for (int i = 0; i < currentQuestion.options.Count; i++)
            {
                var item = currentQuestion.options[i];
                if (item.isCorrect)
                {
                    answer += $"{item.itemText}\n";
                }
            }
            result += $"正确选择项是：{answer}\n";
            return result;
        }
    }
}
