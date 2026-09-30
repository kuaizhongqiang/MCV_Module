# Contract: MenuScrollLogic

Role: numeric scroll state machine of the menu cover flow (inertia, snap, drag velocity sampling); it touches no UI component and is driven by MenuPanel.

Fields:
OnStep:Action  invoked after every stepped frame so MenuPanel can refresh the layout
OnComplete:Action  invoked when the scroll routine finishes
ScrollPhase  enum { Idle, Inertia, Snap }
dampingTime:float  inertia damping length, 0.6
snapDuration:float  snap animation length, 0.3
snapThreshold:float  speed below which snapping starts, 0.05
dragSensitivity:float  drag sensitivity, 3
wheelInitialSpeed:float  initial inertia speed per wheel step, 4
step:float  button height plus spacing, 180; converts pixels into focus units
Focus:float  current focus, fractional while scrolling
Velocity:float  current velocity, negative = upward
Phase:ScrollPhase  current stage
decayPerSec:float  private; exponential decay coefficient derived from dampingTime
snapStart / snapTarget / snapTime:float  private; snap origin, nearest integer target, progress 0..1

Methods:
SetFocus(focus)  hard reposition, velocity 0, phase Idle
ResetVelocity()  zero the velocity only
Stop()  phase Idle + velocity 0
DragUpdate(newFocus, dt)  smooth-sample the frame velocity (Lerp 0.5) -> Focus = newFocus; dt is clamped away from 0
StartInertia(initialVelocity)  set the velocity and derive decayPerSec from dampingTime -> phase Inertia
InertiaStep(dt)  below snapThreshold -> velocity 0, phase Idle, false; else integrate Focus, decay the velocity, true
StartSnap()  snapStart = Focus, snapTarget = nearest int, snapTime = 0 -> phase Snap
SnapStep(dt)  t-squared eased Lerp toward snapTarget; true while running, false and exact target when done
ScrollInertia()  loop InertiaStep (OnStep each frame) then chain into ScrollSnap
ScrollSnap()  StartSnap then loop SnapStep (OnStep each frame) -> OnComplete
Mod(a, n)  static; ring modulo, always non-negative, 0 when n <= 0
ClampOdd(value, min)  static; raise to min, then bump to an odd number
IsPointerInside(target, screenPos)  static; screen point -> local point -> RectTransform rect contains

Notes:
- Plain class and no MonoBehaviour: MenuPanel starts the coroutines and refreshes the layout through OnStep, so this class must never touch UI components.
- decayPerSec comes from Mathf.Log(0.05) / dampingTime, so dampingTime is clamped away from 0 to avoid a division blow-up.
- InertiaStep zeroes the velocity below snapThreshold so the caller switches to snap instead of drifting forever.
- Snap targets the nearest integer focus; Mod and ClampOdd keep callers on the same integer-grid assumption.
- IsPointerInside passes null as the camera (overlay canvas); a world-space canvas would need the real camera.
