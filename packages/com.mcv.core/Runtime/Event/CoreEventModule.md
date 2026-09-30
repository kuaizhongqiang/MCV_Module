# Contract: CoreEventModule

Role: element and step domain event payloads that reference module-domain types; split out of CoreEvent.cs so the module package can carry its own event data.

Fields:
ElementStateChangeEventData.Element:ElementObjBase
InspectionProbeEventData.ProbeType:InspectionProbeType / Point:InspectionElementPointObj / IsContact:bool
InspectionProbeSnapEventData.ProbeType:InspectionProbeType / Point:InspectionElementPointObj / IsSnapped:bool
ProcessChangedEvent.ProcessingIndex:int / Processing:ProcessingHandler
StepPreparedEvent / StepWaitingEvent / StepCompletedEvent  Step:StepHandler, ProcessingIndex:int, StepIndex:int

Methods:
ElementStateChangeEventData(element)
InspectionProbeEventData(probeType, point, isContact)
InspectionProbeSnapEventData(probeType, point, isSnapped)
ProcessChangedEvent(processingIndex, processing)
StepPreparedEvent / StepWaitingEvent / StepCompletedEvent(step, processingIndex, stepIndex)

Notes:
- These payloads live in the module package because they reference module-domain types (ElementObjBase, InspectionElementPointObj, ProcessingHandler, StepHandler); do not move them back to CoreEvent.cs.
- The probe events publish only on state flips, never per frame, so consumers use them directly without a pool.
- Contact and snap differ: contact is merely being inside the detection area, while snap means stuck onto the point while not dragging. Logic that must recognise "really attached" (operation log, snap feedback) subscribes the snap event.
- For the probe events, releasing contact means "left the detection area in space" and is unrelated to releasing the mouse button.
- Step events follow phase order Prepared, Waiting, Completed, each carrying ProcessingIndex and StepIndex for identification.
