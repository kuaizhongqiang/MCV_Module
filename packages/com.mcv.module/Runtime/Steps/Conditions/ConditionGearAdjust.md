# Contract: ConditionGearAdjust

Role: completes when the multimeter knob (step.TargetObj) is turned to step.GearType; function gear only, range is never checked.

Fields:
knob:InspectionMultimeterKnobObj  resolved from step.TargetObj
knobColliders:Collider[]  cached colliders of the knob, toggled per step

Methods:
Type -> ConditionType.GearAdjust
ResetCondition()  override; base + ResolveKnob + SetKnobInteractable(false) -- also the init-time off
OnPrepare()  SetKnobInteractable(false)
Waiting()  ShowAnimationsAtFirstFrame -> ResolveKnob -> SetKnobInteractable(true) -> WaitUntilOrForceComplete(IsOnTargetGear) -> SetKnobInteractable(false)
OnCompleteHide()  SetKnobInteractable(false)
IsOnTargetGear()  knob != null && knob.GearCount > 0 && knob.CurrentGearType == step.GearType
ResolveKnob()  cache step.TargetObj as knob + its colliders; true when already resolved
SetKnobInteractable(on)  enable/disable every collider; warn when the knob has none

Notes:
- Not turnable before this step: ResetCondition (called by ConditionInit) and Prepare disable the colliders, Waiting enables them, completion disables them again. Same idea as InstControlledManager opening clickObj step by step.
- Only colliders are toggled, IsInteractable is deliberately untouched: InteractiveBase.Awake self-subscribes Mo* events only when isInteractable is true, and the Awake order between StepHandler and the knob is undefined -- forcing it false there makes the knob skip its own subscription and never receive MoDown ("cannot turn it"). Disabled colliders also keep GlobalInteractiveMgr's ray off the knob and let the ray pass through to objects behind.
- Re-reads the gear on entering Waiting: when the previous step already left it on the target gear no switch event fires, so waiting on events alone would hang. Same rule as the probe re-read in ConditionMeasurePair.
- Missing or wrong-typed targetObj -> warn and skip; never blocks the flow.
