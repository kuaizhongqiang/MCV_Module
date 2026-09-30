# Contract: ICondition

Role: defines one step condition as a pure C# three-phase coroutine state machine; driven by StepManager yields (Prepare -> Waiting -> Complete).

Fields:
Type:ConditionType  condition kind, 1:1 with the ConditionType enum
Status:StepStutus  state machine: Ready -> Waiting -> Complete

Methods:
ConditionInit(StepHandler step)  one-time init after construction in StepHandler.Awake; idempotent
Prepare()  phase 1: generic show/hide, hide animated objects, subclass hook -> yields
Waiting()  phase 2: subclass interaction loop; returns when satisfied or ForceComplete() -> blocks
Complete()  phase 3: OnCompleteHide, play animations, wait for playback, hideOnComplete -> yields
FastForward()  skip Waiting and jump animations to the last frame (used before a jump) -> yields
ForceComplete()  set Status = Complete so Waiting exits soon; called by NextStep / Skip
ResetCondition()  clear the interrupt flag and stray subscriptions, Status back to Ready; run before every jump

Notes:
- Implementer: ConditionBase (abstract, pure C#) under Steps/Conditions; the framework drives it only through StepManager coroutine yields, so phases must never be called out of order.
- NextStep / Skip interrupt a blocked Waiting cooperatively through ForceComplete(); a Waiting loop must poll the flag or Status, otherwise the coroutine hangs forever.
- Before a jump the framework runs ResetCondition() on every condition; skipping it leaks EventBus subscriptions and stale interrupt flags into the next run.
