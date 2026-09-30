# Contract: ConditionBase

Role: abstract step-condition base; three-phase lifecycle + cooperative interruption + interaction subscription. Plain class, not a MonoBehaviour.

Fields:
step:StepHandler  host step; provides SetObjsActive / animation methods / interaction fields
forceComplete:bool  set by ForceComplete; Waiting loops exit on it
interactionSubscribed:bool  guard for defensive unsubscribe inside Reset
interactionHandler:Action<GlobalInteractionEventData>  cached delegate used for unsubscribe
Status:StepStutus  default Ready

Properties:
Type -> ConditionType  abstract, implemented by subclass
IsForceCompleted -> bool

Methods:
ConditionInit(StepHandler)  store step, then ResetCondition()
ResetCondition()  virtual; clear forceComplete, Status back to Ready, unsubscribe leftovers; StepManager calls it on every condition before a jump
Prepare()  virtual; SetObjsActive() + HideAnimations() + OnPrepare()
OnPrepare()  virtual hook for subclass preparation
Waiting()  abstract; subclass interaction loop
Complete()  virtual; OnCompleteHide() + PlayAnimations() + wait !AnyAnimationPlaying() + HideAnimationsOnComplete()
OnCompleteHide()  virtual hook for subclass cleanup
FastForward()  virtual; yields FastComplete()
FastComplete()  virtual; OnCompleteHide() + StopAtLastFrame() + HideAnimationsOnComplete()
ForceComplete()  set forceComplete + Status = Complete; called by NextStep / SkipCurrentStep
WaitUntilOrForceComplete(Func<bool>)  protected; poll predicate each frame, exit as soon as forceComplete
SubscribeInteraction(Action<GlobalInteractionEventData>)  protected
UnsubscribeInteraction()  protected, idempotent
RaycastHitTarget(InteractiveBase)  protected; manual camera raycast for the Drag / Tool drop point
IsMouseUp()  protected; true when left button released this frame

Notes:
- Every blocking point inside Waiting MUST use WaitUntilOrForceComplete; otherwise NextStep / Skip cannot interrupt and the chain hangs.
- Do not use WaitForSeconds for long waits: it makes Skip feel laggy.
- EventBus.Unsubscribe is idempotent, so ResetCondition may unsubscribe defensively.
- Drag / Tool must detect release via IsMouseUp(), NOT via an Up event: releasing over empty space publishes only a Click with Target = null.
