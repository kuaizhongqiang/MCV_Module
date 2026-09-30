# Contract: InstControlledManager

Role: simplified step runner for lightweight demo scenes that do not use StepManager: a linear "click + play animation" sequence on the structure-model prefab.

Fields:
controlAnim:Animation  [SerializeField] Legacy Animation host; clips must be non-looping or the finish wait hangs
simpleSteps:List<SimpleStepStruct>  [SerializeField] steps executed in list order
s_Instance:InstControlledManager  static scene instance
currentIndex:int  current step index, -1 = not started / finished
executionCoroutine:Coroutine  running coroutine
interactionHandler:Action<GlobalInteractionEventData>  Waiting-phase subscription, kept for unsubscribe
waitClicked:bool  the current step has been clicked
isRunning:bool  running flag
clip:ProjectClip  data of this run, returned with the completion event
Instance  get (FindObjectsByType on demand; never creates a GameObject) / set
Exists:bool  static; use during shutdown
IsRunning / CurrentIndex / StepCount  public read-only views
StepObjState  [enum] Disabled / Clickable / HoverOnly
SimpleStepStruct  [Serializable struct] index / clip / clickObj / delay / animSpeed

Methods:
Awake()  s_Instance = this (last one wins, deliberately no duplicate-destroy); ensure simpleSteps is not null
OnDestroy()  StopSteps() then clear the singleton when it is this
StartSteps(clip = null)  remember clip -> StopSteps -> empty list warns and publishes the completion event -> run ExecuteAll
PlayAuto(speed, onComplete = null)  StopSteps -> empty list warns and invokes the callback -> run PlayAutoAll
StopSteps()  stop the coroutine, unsubscribe, clear waitClicked, SetAllInteractable(Disabled), stop the animation, currentIndex = -1
ExecuteAll()  loop the steps -> ExecuteStep each -> publish the completion event
ExecuteStep(step)  StepPrepare -> StepWaiting -> StepComplete -> wait step.delay
StepPrepare(step)  log progress, SetInteractable(clickObj, Clickable), SampleStartFrame
StepWaiting(step)  no clickObj -> return at once; else subscribe, poll waitClicked, unsubscribe, then set HoverOnly
StepComplete(step)  PlayClipAndWait with NormalizeSpeed(animSpeed)
PlayAutoAll(speed, onComplete)  one frame first, then forward (0->N when speed > 0) or backward (N->0 when speed < 0) playing each clip; clears the coroutine handle before invoking the callback
PlayClipAndWait(step, speed)  set clip and state.speed, normalizedTime 1 for reverse, Play, one frame, then poll IsClipFinished
OnInteraction(data)  Click only; the target must be the current step's clickObj, then waitClicked = true
SampleStartFrame(step, speed)  Play + Sample + Stop to hold the start frame (same convention as StepHandler)
IsClipFinished(state, speed)  isPlaying false wins; otherwise normalizedTime >= 1 forward or <= 0 reverse
NormalizeSpeed(speed)  static; 0 becomes 1
SetAllInteractable(state) / SetInteractable(obj, state)  static; IsInteractable and every Collider.enabled switch together; warn when the object has no collider
UnsubscribeInteraction() / PublishCompleted()  unsubscribe guard / publish StructInteractiveCompletedEvent carrying clip

Notes:
- The lifecycle follows the real step system (Prepare -> Waiting -> Complete + step delay) but only two abilities exist: a click on clickObj and a clip played at animSpeed. Drag / tool / panel / question / line conditions are unsupported, and there is no NextStep, jump or interrupt.
- No resource loading and no pooling: clickObj and controlAnim are assigned by hand in the Inspector.
- Three interaction levels: not yet reached or not running = Disabled (flag and colliders off so rays pass through); current step = Clickable; finished step = HoverOnly (still hoverable for the floating tooltip, but clicks no longer advance anything because only the current Waiting step's clickObj counts). During auto-play everything is Disabled.
- IsInteractable and Collider.enabled must switch together: GlobalInteractiveMgr takes the frontmost collider and then tests IsInteractable, so a flag-only disable lets an earlier part swallow the ray and the target behind becomes unclickable. HoverOnly must keep the collider, otherwise even hovering is lost.
- Awake deliberately does not destroy duplicates: this component sits on the structure-model prefab, not on a unique scene manager, and Destroy takes effect only at the end of the frame -- destroying on duplicate would kill the freshly instantiated model while the old one is still alive in the same frame.
- Only the manual flow publishes StructInteractiveCompletedEvent, because subscribers score on it; auto-play pingpong would publish it every round, so PlayAuto takes a callback instead.
- PlayAutoAll yields one frame before doing anything so it can never complete synchronously inside the StartCoroutine call; otherwise an empty clip list would invoke onComplete on the spot and a pingpong caller with all-empty clips would recurse until the stack blows.
- Reverse playing walks steps from N to 0 and starts every clip at its last frame: playing the clips backwards while iterating forward is a different path from reassembling in reverse order.
- The completion event carries the ProjectClip passed to StartSteps.
