using MCV_Module.Data.Project;
using UnityEngine;

namespace MCV_Module.StepSystem
{
    public abstract class ConditionBase : MonoBehaviour
    {
        [SerializeField] protected string _conditionId;
        [SerializeField] protected ConditionType _conditionType = ConditionType.Default;

        // 由 ConditionFactory 从 ConditionConfig 注入
        public string TargetId { get; set; }
        public string ExtraParam { get; set; }

        protected StepDirector Director { get; private set; }
        public string ConditionId => _conditionId;
        public ConditionType ConditionType => _conditionType;

        protected virtual void Start()
        {
            Director = GetComponentInParent<StepDirector>();
        }

        protected void Complete()
        {
            if (Director != null)
                Director.CompleteCurrentStep();
        }

        public virtual void ResetCondition() { }
        public abstract bool IsMet();

        /// <summary>获取类型安全的额外参数</summary>
        protected T GetExtraParam<T>() where T : class
        {
            if (string.IsNullOrEmpty(ExtraParam)) return null;
            try { return JsonUtility.FromJson<T>(ExtraParam); }
            catch { return null; }
        }
    }
}
