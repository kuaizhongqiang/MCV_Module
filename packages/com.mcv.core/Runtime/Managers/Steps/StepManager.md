# Contract: StepManager

Role: the step director; drives every processing/step with a coroutine over Prepare -> Waiting -> Complete, publishes state events, and handles step delay, next, jump, skip.

Fields:
instance:StepManager  static singleton, bound in Awake
stepDelayTime:float  [SerializeField] delay between steps (s), 0.5
processingDelayTime:float  [SerializeField] delay between processings (s), 0.3
canvasRebuildWaitTimeout:float  [SerializeField] max wait for the content-page canvas rebuild before starting the chain (s); 0 = start at once, 1.5
processingHandlers:List<ProcessingHandler>  child handlers collected in Awake
currentProcessingIndex / currentStepIndex:int  cursors, -1 before the first start
currentStep:StepHandler  the step being executed
lifecycle:StepLifecycle  Idle | Prepare | Waiting | Complete
executionCoroutine:Coroutine  the running ExecuteAll / JumpTo coroutine
isFinished:bool  set by EndChain; stops ExecuteAll / JumpTo from advancing again
CurrentProcessing / CurrentStep / CurrentLifecycle / IsRunning  read-only views

Methods:
Awake()  instance = this -> collect child ProcessingHandler components
DelayInit()  subscribe StepNextRequestEvent + StepJumpRequestEvent + ProcessingJumpRequestEvent -> ConditionInit on every step -> isInit -> WaitCanvasRebuild -> StartExecution
WaitCanvasRebuild()  yield one frame, then wait while GlobalUIMgr.IsSwitching && CanvasRebuildVersion unchanged && under the timeout; logs which path was taken
OnDestroy()  stop the coroutine, unsubscribe the three events, clear the singleton
StartExecution()  default both cursors to 0, then run ExecuteAll
JumpToStep(processingIndex, stepIndex)  clamp both indices, then run JumpTo
JumpToProcessing(i) / JumpToStep(stepIndex) / SetProcessing(i)  convenience wrappers
PrevStep()  previous step in the processing, else the last step of the previous processing
StopExecution()  stop the coroutine and reset to Idle
NextStep()  ForceComplete on the current condition, only while in Prepare/Waiting
SkipCurrentStep() / CompleteCurrentStep()  aliases of NextStep
OnStepNextRequested / OnStepJumpRequested / OnProcessingJumpRequested  event entry points
ExecuteAll()  walk processings and steps, publish ProcessChangedEvent, execute each step, finish with AllStepsCompletedEvent
ExecuteStep(step, p, s)  no condition -> publish Prepared/Waiting/Completed then delay; else run Prepare -> Waiting -> Complete with a publish after each, then EndChain for a Finish step
IsFinishStep(step)  static; Type == ConditionType.Finish
EndChain()  isFinished = true -> Idle -> publish AllStepsCompletedEvent
JumpTo(targetProcess, targetStep)  PrepareAllConditions -> fast-forward everything before the target -> execute from the target on
PrepareAllConditions()  ResetCondition + Prepare for every condition

Notes:
- Why the canvas wait exists: the inspection prefab is instantiated immediately on the task-change event, while the canvas rebuild waits for the previous canvas fade-out (about 0.3s). Starting earlier means the Start step's StepUIPanel is destroyed by ClearPanels(), the panel never shows and the controller waits forever for its confirm event. The signal is GlobalUIMgr.CanvasRebuildVersion, not a fixed delay.
- Three ways out of that wait so it can never hang: the first frame is yielded, "not switching" means the canvas is already stable, and the timeout backs it up.
- A Finish step runs all three phases first (including a confirm panel in Waiting) and only then ends the chain; the old behaviour skipped the phases, so a panel configured on Finish never appeared.
- JumpTo resets and prepares every condition first, so animation and interaction state stay consistent (aligned with Tuanjie).
- isFinished is checked after every step in both ExecuteAll and JumpTo, so AllStepsCompletedEvent is published exactly once.
- StepLifecycle is a public enum declared next to this class.
