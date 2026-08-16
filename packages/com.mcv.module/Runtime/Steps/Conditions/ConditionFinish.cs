using System.Collections;
using MCV_Module.Models;

namespace MCV_Module.Steps
{
    /// <summary>
    /// 完成条件 —— 作为每个 Processing 的终结步骤。
    /// 特判在 StepManager.ExecuteStep：识别到 Finish 类型后直接发布 AllStepsCompletedEvent 并结束，
    /// 不执行 Prepare/Waiting/Complete 三阶段（对齐 Tuanjie）。
    /// </summary>
    public class ConditionFinish : ConditionBase
    {
        public override ConditionType Type => ConditionType.Finish;

        public override IEnumerator Waiting()
        {
            // 正常情况下不会走到这里（StepManager 特判 Finish 后提前结束）
            yield break;
        }
    }
}
