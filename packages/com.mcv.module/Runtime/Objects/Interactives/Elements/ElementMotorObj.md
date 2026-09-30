# Contract: ElementMotorObj

Role: electric motor (M); start and stop through the run animation, with an optional target-speed overload.

Fields:
points:List<ElementPointObj>  runtime only; its terminals, exposed as Points
runAnimation:ElementRunAnimation  [SerializeField]; rebuilt in Awake with `this`
Type  ElementType.Motor

Methods:
Awake()  base -> rebuild runAnimation(this, runObj, rotationAxis, runSpeed, speedChangeDuration) -> Reset() to capture the initial state
MotorRun()  runAnimation.Play()
MotorRun(float speed)  runAnimation.Play(speed); speed in degrees per second, eased to the target
MotorStop()  runAnimation.Stop()

Notes:
- Reset() must run in Awake, otherwise the first Play is a no-op and the first click does nothing.
- runAnimation is rebuilt at runtime; the serialized value only feeds runObj / rotationAxis / runSpeed / speedChangeDuration.
