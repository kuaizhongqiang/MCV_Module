using System.Collections.Generic;
using MCV_Module.Data.Project;
using UnityEngine;

namespace MCV_Module.StepSystem
{
    /// <summary>
    /// Condition 工厂 —— 按 ConditionType 创建对应的 Condition 实例。
    /// 支持 P0S0 快进：重置到工序0步骤0，快速执行到目标步骤。
    /// </summary>
    public class ConditionFactory : MonoBehaviour
    {
        [SerializeField] private StepDirector _director;

        private readonly Dictionary<ConditionType, System.Type> _conditionMap = new Dictionary<ConditionType, System.Type>
        {
            { ConditionType.Default, typeof(DefaultCondition) },
            { ConditionType.Click, typeof(ClickCondition) },
            { ConditionType.Drag, typeof(DragCondition) },
            { ConditionType.Tool, typeof(ToolCondition) },
            { ConditionType.UI, typeof(UICondition) },
            { ConditionType.Question, typeof(QuestionCondition) },
            { ConditionType.LineConnect, typeof(LineConnectCondition) },
            { ConditionType.Finish, typeof(FinishCondition) },
        };

        /// <summary>按配置创建 Condition 组件</summary>
        public ConditionBase CreateCondition(ConditionConfig config)
        {
            if (config == null) return null;

            if (_conditionMap.TryGetValue(config.type, out var type))
            {
                var condition = gameObject.AddComponent(type) as ConditionBase;
                if (condition != null)
                {
                    condition.transform.SetParent(transform);
                    return condition;
                }
            }
            return null;
        }

        /// <summary>
        /// P0S0 快进：重置所有 Condition 状态，快速执行到目标步骤。
        /// </summary>
        public void FastForwardTo(int targetProcessingIndex, int targetStepIndex)
        {
            // 重置所有 Condition
            var conditions = GetComponentsInChildren<ConditionBase>();
            foreach (var c in conditions)
                c.ResetCondition();

            // 跳转到目标步骤
            if (_director != null)
                _director.JumpToStep(targetProcessingIndex, targetStepIndex);
        }
    }
}
