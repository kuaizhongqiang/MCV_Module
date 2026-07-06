using System.Collections.Generic;
using MCV_Module.Data.Project;
using UnityEngine;

namespace MCV_Module.StepSystem
{
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

        /// <summary>按配置创建 Condition 组件，并注入 config 数据</summary>
        public ConditionBase CreateCondition(ConditionConfig config)
        {
            if (config == null) return null;

            if (_conditionMap.TryGetValue(config.type, out var type))
            {
                var condition = gameObject.AddComponent(type) as ConditionBase;
                if (condition != null)
                {
                    condition.transform.SetParent(transform);
                    // 注入 ConditionConfig 数据
                    condition.TargetId = config.targetId;
                    condition.ExtraParam = config.extraParam;
                    return condition;
                }
            }
            return null;
        }

        /// <summary>P0S0 快进</summary>
        public void FastForwardTo(int targetProcessingIndex, int targetStepIndex)
        {
            var conditions = GetComponentsInChildren<ConditionBase>();
            foreach (var c in conditions)
                c.ResetCondition();

            if (_director != null)
                _director.JumpToStep(targetProcessingIndex, targetStepIndex);
        }
    }
}
