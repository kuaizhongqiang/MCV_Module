using MCV_Module.Event;
using MCV_Module.StepSystem;
using MCV_Module.UI.Panels;
using UnityEngine;

namespace MCV_Module.Controller
{
    public class StepQuestionController : ControllerBase<StepQuestionPanel>
    {
        private QuestionCondition _currentQuestionCondition;

        protected override void OnViewBound()
        {
            EventBus<StepWaitingEvent>.Subscribe(OnStepWaiting);
        }

        private void OnStepWaiting(StepWaitingEvent evt)
        {
            // 查找当前步骤的 QuestionCondition
            var directors = FindObjectsByType<StepDirector>(FindObjectsSortMode.None);
            if (directors.Length == 0) return;

            var step = directors[0].CurrentStep;
            if (step == null) return;

            // 查找 QuestionCondition
            var conditions = directors[0].GetComponentsInChildren<QuestionCondition>();
            foreach (var c in conditions)
            {
                _currentQuestionCondition = c;
                View.ShowQuestion(step.description, OnAnswerSubmitted);
                return;
            }
        }

        private bool OnAnswerSubmitted(string answer)
        {
            if (_currentQuestionCondition == null) return false;
            return _currentQuestionCondition.SubmitAnswer(answer);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventBus<StepWaitingEvent>.Unsubscribe(OnStepWaiting);
        }
    }
}
