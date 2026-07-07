using MCV_Module.Data.Project;
using UnityEngine;

namespace MCV_Module.StepSystem
{
    /// <summary>默认条件 —— 自动满足，用于 Auto 类型步骤</summary>
    public class DefaultCondition : ConditionBase
    {
        [SerializeField] private ConditionType _type = ConditionType.Default;

        public override bool IsMet() => true;
    }
}
