using MCV_Module.Models;

namespace MCV_Module.Steps
{
    /// <summary>UI 信息条件：弹说明面板，点确认即完成。</summary>
    public class ConditionUI : ConditionStepPanelBase
    {
        public override ConditionType Type => ConditionType.UI;
    }
}
