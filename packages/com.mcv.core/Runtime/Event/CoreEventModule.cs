using MCV_Module.Models;
using MCV_Module.Steps;

namespace MCV_Module.Event
{
    // ── 步骤域事件载荷（见 CoreEvent.cs）──

    /// <summary>当前进程变化事件数据（进入新进程时发布）</summary>
    public class ProcessChangedEvent
    {
        public int ProcessingIndex;
        public ProcessingHandler Processing;

        public ProcessChangedEvent(int processingIndex, ProcessingHandler processing)
        {
            ProcessingIndex = processingIndex;
            Processing = processing;
        }
    }

    /// <summary>步骤初始化事件（Prepare 阶段）</summary>
    public class StepPreparedEvent
    {
        public StepHandler Step;
        public int ProcessingIndex;
        public int StepIndex;

        public StepPreparedEvent(StepHandler step, int processingIndex, int stepIndex)
        {
            Step = step;
            ProcessingIndex = processingIndex;
            StepIndex = stepIndex;
        }
    }

    /// <summary>步骤等待执行事件（Waiting 阶段，等待条件满足）</summary>
    public class StepWaitingEvent
    {
        public StepHandler Step;
        public int ProcessingIndex;
        public int StepIndex;

        public StepWaitingEvent(StepHandler step, int processingIndex, int stepIndex)
        {
            Step = step;
            ProcessingIndex = processingIndex;
            StepIndex = stepIndex;
        }
    }

    /// <summary>步骤执行完成事件</summary>
    public class StepCompletedEvent
    {
        public StepHandler Step;
        public int ProcessingIndex;
        public int StepIndex;

        public StepCompletedEvent(StepHandler step, int processingIndex, int stepIndex)
        {
            Step = step;
            ProcessingIndex = processingIndex;
            StepIndex = stepIndex;
        }
    }
}
