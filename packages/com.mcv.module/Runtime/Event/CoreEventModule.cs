using MCV_Module.Models;
using MCV_Module.Objects.Interactives.Elements;
using MCV_Module.Objects.Interactives.TaskObj;
using MCV_Module.Steps;

namespace MCV_Module.Event
{
    // ── 元件/步骤域事件载荷（引用 module 域类型，随 module 包拆分，见 CoreEvent.cs）──

    /// <summary>元件状态变化事件数据（断路器/按钮等元件状态改变时发布，供管理器重算电路等逻辑）。</summary>
    public class ElementStateChangeEventData
    {
        /// <summary>发生状态变化的元件</summary>
        public ElementObjBase Element;

        public ElementStateChangeEventData(ElementObjBase element)
        {
            Element = element;
        }
    }

    // WHY: 解除是"空间上离开检测区"，与鼠标松开无关；只在接触状态翻转时发一次，不逐帧发。
    /// <summary>表笔接触事件数据（InspectionProbeObj 的检测区碰到待检测点 / 离开时发布）。</summary>
    public class InspectionProbeEventData
    {
        /// <summary>红表笔 / 黑表笔</summary>
        public InspectionProbeType ProbeType;

        /// <summary>被接触（或解除接触）的待检测点</summary>
        public InspectionElementPointObj Point;

        /// <summary>true = 接触，false = 解除</summary>
        public bool IsContact;

        public InspectionProbeEventData(InspectionProbeType probeType, InspectionElementPointObj point, bool isContact)
        {
            ProbeType = probeType;
            Point = point;
            IsContact = isContact;
        }
    }

    // WHY: 吸附 = 接触且未拖拽；拖拽途中蹭到检测点只算"接触"不算吸附，要认"真正贴住了"的逻辑须订阅本事件而非接触事件。
    /// <summary>表笔吸附事件数据（InspectionProbeObj 的吸附态翻转时发布，只在状态翻转时发一次）。</summary>
    public class InspectionProbeSnapEventData
    {
        /// <summary>红表笔 / 黑表笔</summary>
        public InspectionProbeType ProbeType;

        /// <summary>吸附住（或脱离吸附）的待检测点</summary>
        public InspectionElementPointObj Point;

        /// <summary>true = 吸附住，false = 脱离吸附</summary>
        public bool IsSnapped;

        public InspectionProbeSnapEventData(InspectionProbeType probeType, InspectionElementPointObj point, bool isSnapped)
        {
            ProbeType = probeType;
            Point = point;
            IsSnapped = isSnapped;
        }
    }

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
