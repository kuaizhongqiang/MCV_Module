# Contract: RoomShowObj

Role: exhibit float and hover behaviour in the room; floats along one axis, decelerates with inertia on hover and accelerates back on exit, click left as a hook.

Fields:
uiConetnt:string  [SerializeField] click content key (spelling is historical and must not be changed); no consumer yet
animMoveSpeed:float  [SerializeField] float speed, units per second along moveAxis
moveAxis:ObjAxis  [SerializeField] float axis, only this component moves; default Y
maxPos:float  [SerializeField] upper bound as an absolute local coordinate on that axis; default 1
minPos:float  [SerializeField] lower bound as an absolute local coordinate on that axis; default -1
ifRondomPosStart:bool  [SerializeField] random starting phase; default true
hoverSpeedFactor:float  [SerializeField,Range(0,1)] speed multiplier while hovered; default 0
inertiaDuration:float  [SerializeField] speed-change easing duration in seconds; default 0.35
easePower:float  [SerializeField,Range(1,4)] easing strength; default 2
moveCoroutine:Coroutine  the float coroutine
mouseInteractCoroutine:Coroutine  the inertia coroutine
m_BaseLocalPos:Vector3  local position recorded in Awake
m_SpeedScale:float  current speed multiplier (1 = normal)
m_SpeedVelocity:float  SmoothDamp working value
m_Placed:bool  whether the first placement already happened

Methods:
Awake()  base -> record the base position
OnEnable()  reset the multiplier -> restart the float coroutine
OnDisable() / StopMotion()  stop both coroutines and reset the inertia
MoveRoutine()  warn and stop on invalid params; random start -> clamp into range -> advance by the multiplier with easing -> reverse
EaseSquare(float)  easing with zero derivative at both ends; p = 1 degenerates to linear
SetAxisValue(float) / GetAxisValue()  write / read only the moveAxis component
StartInertia(float) / InertiaRoutine(float)  push the multiplier toward the target (single segment at a time)
MoEnterEvent()  StartInertia(hoverSpeedFactor)
MoExitEvent()  StartInertia(1f)
MoClickEvent()  forward to OnClick
OnClick()  virtual, empty hook
UiContent() / SpeedScale()  read-only accessors

Notes:
- minPos / maxPos are absolute local coordinates on that axis, not offsets from the start position; only the moveAxis component is written and the other two stay at their Awake values.
- The coroutine does not resume by itself after OnDisable, so every OnEnable must restart it.
- Presentation only: this class knows nothing about bundles and makes no navigation decisions.
