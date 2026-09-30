# Contract: StepHandler

Role: single step node; holds one step's data and its runtime condition; collected by ProcessingHandler, driven by StepManager.

Fields (serialized):
id:string  stable step id; empty -> generated as Step_{processingIndex}_{index}
displayName:string  overwritten in Awake to {id}_{Type}
description:string  step copy (was the TipsController fallback; TipsController removed 2026-09-30)
conditionType:ConditionType  selects the ConditionBase subclass
showObjs/hideObjs:List<GameObject>  applied once in Prepare
animations:List<StepAnimation>  each = {animation, clip, hideOnComplete}
tipsId:string  -> StepContentData 的 StepTipsData.id
audioId:string  reserved, no consumer
targetObj:InteractiveBase  Click / Drag / Tool
dragObj:InteractiveBase  Drag
usingId:string  Tool / UI / Start / Finish / Question
lines:List<InteractiveBase>  reserved (LineConnect condition removed 2026-09-30)
points:List<InteractiveBase>  reserved (MeasurePair condition removed 2026-09-30)
gearType:MultimeterGearType  reserved (GearAdjust condition removed 2026-09-30)

Fields (runtime, [NonSerialized]):
condition:ConditionBase  instance created from conditionType in Awake

Methods:
Awake()  build id/displayName -> CreateCondition() -> condition.ConditionInit(this)
SetObjsActive()  activate showObjs, deactivate hideObjs
HideAnimations()  hide every animation object (Prepare reset)
ShowAnimationsAtFirstFrame()  show and park each animation at normalizedTime 0
PlayAnimations()  play all animations
StopAtLastFrame()  park each animation at normalizedTime 1 (FastComplete)
AnyAnimationPlaying() -> bool  Complete waits on this
HideAnimationsOnComplete()  hide only entries with hideOnComplete
CreateCondition()  private switch over ConditionType

Notes:
- Animations must be Legacy Animation: exact frame control uses Play + Sample + Stop.
- Explicit id wins; without it the id follows hierarchy order, so reordering steps changes it.
- displayName is overwritten at runtime and must not be used as UI copy.
- Serialized data lives in the prefab, never in JSON.
