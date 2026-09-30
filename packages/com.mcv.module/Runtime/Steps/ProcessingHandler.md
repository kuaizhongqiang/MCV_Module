# Contract: ProcessingHandler

Role: single processing node; collects its child StepHandlers; data source for StepManager.

Fields (serialized):
id/displayName/description:string  identity of the processing

Fields (runtime):
steps:List<StepHandler>  collected from children in Awake

Methods:
Awake()  collect child StepHandler into steps; rename gameObject to Processing_{siblingIndex}
GetStep(int index) -> StepHandler  bounds-checked, returns null when out of range
GetStep(string stepId) -> StepHandler  linear scan by Id, returns null when missing
GetSteps() -> List<StepHandler>  live list
StepCount -> int  property

Notes:
- steps order == child order == execution order; reordering children changes execution order.
