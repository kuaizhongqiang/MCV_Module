using MCV_Module.Models;

namespace MCV_Module.Steps
{
    /// <summary>开始条件：流程起始标记，弹说明面板，确认后进下一步。</summary>
    public class ConditionStart : ConditionStepPanelBase
    {
        public override ConditionType Type => ConditionType.Start;
    }
}
