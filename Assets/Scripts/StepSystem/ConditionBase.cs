using MCV_Module.Data.Project;
using UnityEngine;

namespace MCV_Module.StepSystem
{
    /// <summary>
    /// 条件基类 —— 每个 Step 内的交互检测单元。
    /// 子类实现具体检测逻辑，满足条件时调用 Complete()。
    /// </summary>
    public abstract class ConditionBase : MonoBehaviour
    {
        [SerializeField] protected string conditionId;
        [SerializeField] protected ConditionType conditionType = ConditionType.Default;

        protected StepDirector Director { get; private set; }
        public string ConditionId => conditionId;
        public ConditionType ConditionType => conditionType;

        protected virtual void Start()
        {
            Director = GetComponentInParent<StepDirector>();
        }

        /// <summary>条件成立，通知 Director 完成当前步骤</summary>
        protected void Complete()
        {
            if (Director != null)
                Director.CompleteCurrentStep();
        }

        /// <summary>重置条件状态（P0S0 快进时调用）</summary>
        public virtual void ResetCondition() { }

        /// <summary>检测条件是否已满足</summary>
        public abstract bool IsMet();
    }
}
