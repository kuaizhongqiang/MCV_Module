# Contract: ElementAnimation (ElementRotationAnimation, ElementRunAnimation, ElementMoveAnimation, ElementRotationStruct)

Role: per-component animation helpers — one-shot rotation to a tag's target angle; continuous spin with speed ramping; slide between open and closed limits.

Fields:
ElementRotationAnimation.rotateObj:Transform  [SerializeField] rotated transform
ElementRotationAnimation.rotationStructs:List<ElementRotationStruct>  [SerializeField] per-tag configs
ElementRotationAnimation.element:ElementObjBase  coroutine host (runtime)
ElementRotationAnimation.coroutine:Coroutine  running rotation coroutine
ElementRotationStruct.animTag:string  lookup key
ElementRotationStruct.rotationAxis:ObjAxis  local axis to rotate
ElementRotationStruct.rotationLimitation:Vector2  x = start, y = target angle in degrees
ElementRotationStruct.duration:float  seconds; <= 0 jumps instantly
ElementRunAnimation.runObj:Transform  [SerializeField] spun transform
ElementRunAnimation.rotationAxis:ObjAxis  [SerializeField] default Z
ElementRunAnimation.runSpeed:float  [SerializeField] degrees per second, default 500
ElementRunAnimation.speedChangeDuration:float  [SerializeField] ramp seconds, default 2
ElementRunAnimation.element / runCoroutine / speedCoroutine / defaultSpeed / initialRotation / captured  runtime state
ElementMoveAnimation.moveObj:Transform  [SerializeField] moved transform
ElementMoveAnimation.moveLimitation:Vector2  [SerializeField] x = open position, y = closed position (local units)
ElementMoveAnimation.duration:float  [SerializeField] seconds, default 0.2
ElementMoveAnimation.moveAxis:ObjAxis  [SerializeField] default Z
ElementMoveAnimation.element / moveCoroutine / open / captured / initialPosition / initialOpen  runtime state

Methods:
ElementRotationAnimation.Play(animTag, onComplete)  find the config -> SetRotation when duration <= 0, else -> Rotate
Rotate(data, onComplete) / RotateCoroutine(data, onComplete)  stop the previous coroutine -> lerp the shortest angular delta -> invoke onComplete
GetCurrentRotation / GetTargetRotation(data)  read the axis euler / rotationLimitation.y
SetRotation(data, angle)  write one local euler component
ElementRunAnimation.Play()  capture the initial state -> cancel the speed ramp, restore the default speed -> start RunningCoroutine
ElementRunAnimation.Play(speed)  Play() then SetSpeed(speed)
ElementRunAnimation.Stop() / StopRunning()  ramp the speed to 0 / stop the coroutine keeping the speed
ElementRunAnimation.Reset()  stop everything -> restore the initial rotation and default speed
ElementRunAnimation.SetSpeed(speed)  restart SpeedChangeCoroutine
ElementMoveAnimation.CaptureInitial()  infer open from the distance to both limits
ElementMoveAnimation.Reset()  stop the move -> restore the initial position and open state
ElementMoveAnimation.SetMove(isOpen)  restart MoveCoroutine
ElementMoveAnimation.MoveCoroutine(isOpen)  instant when duration <= 0, else lerp applied -> target
ElementMoveAnimation.GetCurrentLocalPos / GetTargetLocalPos / SetLocalPos  read and write one local position component

Notes:
- RotateCoroutine uses Mathf.DeltaAngle so rotation takes the shortest path; dropping it makes the object spin a full turn whenever the euler crosses 0/360.
- Play() must cancel speedCoroutine and restore defaultSpeed first: after Stop() ramped the speed to 0, a plain Play() would never move again.
- CaptureInitial() is lazy behind a captured flag because Unity deserialization order makes field values unreliable at construction time.
- All coroutines run on the owning ElementObjBase; a null rotateObj / runObj / moveObj is a silent no-op.
