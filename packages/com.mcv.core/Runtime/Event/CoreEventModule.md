# Contract: CoreEventModule

Role: step-domain event payloads split out of CoreEvent.cs so the module package can carry its own event data.

Fields:
ProcessChangedEvent.ProcessingIndex:int / Processing:ProcessingHandler
StepPreparedEvent / StepWaitingEvent / StepCompletedEvent  Step:StepHandler, ProcessingIndex:int, StepIndex:int

Methods:
ProcessChangedEvent(processingIndex, processing)
StepPreparedEvent / StepWaitingEvent / StepCompletedEvent(step, processingIndex, stepIndex)

Notes:
- 2026-09-30 cleanup: the element/probe payloads (ElementStateChangeEventData, InspectionProbeEventData, InspectionProbeSnapEventData) were removed together with the element business layer; only the step-domain payloads remain.
- Step events follow phase order Prepared, Waiting, Completed, each carrying ProcessingIndex and StepIndex for identification.
