using UnityEngine;

namespace MCV_Module.StepSystem
{
    /// <summary>答题条件 —— 回答正确后满足</summary>
    public class QuestionCondition : ConditionBase
    {
        [SerializeField] private string _questionId;
        [SerializeField] private string _correctAnswer;
        private bool _answered;

        public override bool IsMet() => _answered;

        public override void ResetCondition()
        {
            _answered = false;
        }

        /// <summary>外部调用：提交答案检测</summary>
        public bool SubmitAnswer(string answer)
        {
            if (!_answered && answer == _correctAnswer)
            {
                _answered = true;
                Complete();
                return true;
            }
            return false;
        }
    }
}
