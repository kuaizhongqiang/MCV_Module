using System.Collections.Generic;
using UnityEngine;

namespace MCV_Module.Steps
{
    /// <summary>
    /// 进程节点 —— 承载一个进程的数据（原 ProcessingData 已融合进本组件），
    /// 收集子 StepHandler，作为 StepManager（步骤导演）的数据源。
    /// </summary>
    public class ProcessingHandler : MonoBehaviour
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField] string description;

        List<StepHandler> steps = new List<StepHandler>();

        /// <summary>本进程的步骤数</summary>
        public int StepCount => steps.Count;

        void Awake()
        {
            // 收集子 StepHandler
            steps.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var sh = transform.GetChild(i).GetComponent<StepHandler>();
                if (sh != null) steps.Add(sh);
            }
            int index = transform.GetSiblingIndex();
            string name = $"Processing_{index}";
            gameObject.name = name;
        }

        public StepHandler GetStep(int index)
        {
            if (index < 0 || index >= steps.Count) return null;
            return steps[index];
        }

        public StepHandler GetStep(string stepId)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].Id == stepId) return steps[i];
            }
            return null;
        }

        public List<StepHandler> GetSteps() => steps;
    }
}
