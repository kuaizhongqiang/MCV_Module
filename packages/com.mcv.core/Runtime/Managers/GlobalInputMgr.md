# Contract: GlobalInputMgr

Role: input-controller registry + mouse idle/moving judge; publishes MouseMoveStateEventData only on state flips.

Fields:
idleJudgeTime:float  [SerializeField] idle window in seconds, default 0.1
idleMoveThreshold:float  [SerializeField] accumulated pixel delta that counts as moving, default 0.5
m_ControllerDict:Dictionary<string, InputControllerBase>  name -> registered controller
mouse:Mouse  cached Mouse.current
idleTimer:float  how long the delta has stayed under the threshold
moveDistance:float  accumulated delta of the current window
isIdle:bool  current state; false = moving

Methods:
DelayInit()  one frame -> isInit = true
Update()  accumulate mouse delta -> SetIdle(true/false)
SetIdle(idle)  publish only on flip
RegisterController(name, controller)  static; ignored when the name is already taken
UnregisterController(name)  static
GetController<T>()  static; lookup by typeof(T).Name

Notes:
- Subscribers are GlobalInteractiveMgr (skips raycasts while idle) and TaskPrinciplePanel (auto-hides its controls).
- Uses unscaledDeltaTime, so the judge keeps working at timeScale 0.
- No mouse device (touch-only) -> Update returns early and the last state is kept.
