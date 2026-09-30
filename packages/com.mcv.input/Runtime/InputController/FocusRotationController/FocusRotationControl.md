# Contract: FocusRotationControl

Role: focus-orbit camera; orbits a target with right-drag plus inertia, smooth-follows a new target, resets on double-click and drives the real cameras' FOV.

Fields:
_target:Transform  the focus target
inertiaTime:float  inertia decay time
distance:float  orbit radius
resetDuration:float  double-click reset duration
doubleClickInterval:float  max double-click gap
_cameras:Camera[]  the real cameras parented under this object
_smoothFollowCoroutine / _resetBackCoroutine:Coroutine  running transitions
_lastLeftClickTime:float  last left-click time (unscaled)
_yaw / _pitch / _yawVelocity / _pitchVelocity:float  orbit angles and inertia
_isTransitioning / _freezeOrbit:bool  transition and one-frame freeze flags

Methods:
Awake()  cache cameras, seed defaultFov, record startPos / startRot
Start()  -> InitializeOrbit(target)
Update()  -> HandleResetInput, then ZoomHandle / HandleRot / HandlePos unless transitioning
InitializeOrbit(target)  derive yaw, pitch and distance from startPos
TryGetOrbit(camPos, target, out yaw, out pitch, out radius)  derive orbit angles and radius from the camera world position
ApplyOrbit(target)  place the camera on the orbit and look at the target
HandlePos()  -> ApplyOrbit each frame
ApplyResetFrame(t, fromYaw, fromPitch, fromDistance)  one frame of the reset transition along the orbit
HandleRot()  right-drag rotation plus inertia decay, clamps pitch
HandleResetInput()  double left-click -> ResetPosSmooth
ResetPos()  instant reset plus FOV restore and re-aim
ResetPosSmooth()  -> SmoothResetBack coroutine
SmoothResetBack()  eased reset of pose and FOV over resetDuration
SmoothFollow()  0.5s transition to look straight at the target, keeping radius and side
Transport(Transform)  set the target -> SmoothFollow
ZoomHandle() / ZoomHandleDelay() / TryRestoreDefaultFov() / ZoomRestoreCoroutine()  FOV zoom on the real cameras
SetAllCamerasFov(fov)  write FOV to all cameras, caching defaultFov on the first set

Notes:
- TryGetOrbit must use atan2(-dir.x, -dir.z) to stay consistent with ApplyOrbit; atan2(dir.x, dir.z) is 180 degrees off and flips the camera to the far side of the target.
- After any transition the yaw and pitch must be carried over from the transition's own math, never re-derived from the resulting transform, for the same 180-degree reason.
- The start orbit angle is reverse-derived from the current pose because an interrupted SmoothFollow may have left yaw and pitch inconsistent with the current transform.
- Reset interpolation moves the position only and lets ApplyOrbit aim the camera: interpolating position and rotation separately takes the camera off the target and the target leaves the view.
- The real cameras are driven directly, so defaultFov is seeded in Awake; the base-class default only fits Cinemachine virtual cameras.
