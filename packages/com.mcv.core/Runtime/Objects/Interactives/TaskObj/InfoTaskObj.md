# Contract: InfoTaskObj

Role: info display object; keeps spinning around one axis, decelerating to a stop on hover and accelerating back to the target speed on exit.

Fields:
rotateSpeed:float  [SerializeField] target speed in degrees per second; default 50
InertiaTime:float  [SerializeField] inertia duration in seconds; default 0.5
rotateAxis:ObjAxis  [SerializeField] rotation axis; default Y
currentSpeed:float  current speed in degrees per second, eased
rotateCoroutine:Coroutine  the spin coroutine
inertiaCoroutine:Coroutine  the inertia coroutine

Methods:
SetObjRotate(bool)  spin on: start the spin coroutine and accelerate; off: decelerate to a stop
SetSpeed(float)  ease the speed to the target
InertiaCoroutine(float)  linear easing; stops the spin when it reaches 0
RotateCoroutine()  spin around rotateAxis at currentSpeed
StopRotate()  stop the spin coroutine immediately (speed unchanged)
OnEnable()  SetObjRotate(true)
OnDisable()  StopRotate, stop the inertia coroutine, clear currentSpeed
MoEnterEvent()  SetObjRotate(false)
MoExitEvent()  SetObjRotate(true)

Notes:
- Coroutine driven, no Update.
- Disabling the object terminates its coroutines automatically, so OnDisable must clear the state by hand or the next enable assumes the coroutine is still running.
