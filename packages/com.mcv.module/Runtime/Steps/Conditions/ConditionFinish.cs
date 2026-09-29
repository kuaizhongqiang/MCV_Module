using MCV_Module.Models;

namespace MCV_Module.Steps
{
    /// <summary>完成条件：终结步骤，弹说明面板，确认后整条链结束。</summary>
    public class ConditionFinish : ConditionStepPanelBase
    {
        public override ConditionType Type => ConditionType.Finish;
    }
}
