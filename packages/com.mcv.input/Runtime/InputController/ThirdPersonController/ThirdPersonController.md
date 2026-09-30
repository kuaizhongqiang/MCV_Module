# Contract: ThirdPersonController

Role: third-person player; handles move and sprint with smooth turning, jump and gravity, ground probing, camera aim, footstep and landing audio, and teleporting.

Fields:
MoveSpeed / SprintSpeed / SpeedChangeRate:float  [Tooltip] movement tuning (default 2 / 5.335 / 10)
RotationSmoothTime:float  [Tooltip][Range(0,0.3)] turn smoothing, default 0.12
LandingAudioClip:AudioClip / FootstepAudioClips:AudioClip[] / FootstepAudioVolume:float  audio assets and [Range(0,1)] volume
JumpHeight / Gravity / JumpTimeout / FallTimeout:float  [Tooltip] jump apex, per-character gravity, jump cooldown, free-fall delay
Grounded:bool  [Tooltip] grounded result, self-computed rather than CharacterController.isGrounded
GroundedOffset / GroundedRadius:float  [Tooltip] ground probe sphere offset and radius
GroundLayers:LayerMask  [Tooltip] the layers treated as ground
CinemachineCameraTarget:GameObject  [Tooltip] the camera follow target
CameraAngleOverride / LockCameraPosition:bool  [Tooltip] extra pitch and camera position lock
ResetVerticalVelocityOnTeleport / ImmediateGroundedCheck:bool  [Tooltip] post-teleport behaviour
_cinemachineTargetYaw / _cinemachineTargetPitch / _speed / _animationBlend / _targetRotation / _rotationVelocity / _verticalVelocity / _terminalVelocity  movement and camera state
_jumpTimeoutDelta / _fallTimeoutDelta  countdown timers
_animIDSpeed / _animIDGrounded / _animIDJump / _animIDFreeFall / _animIDMotionSpeed:int  hashed animator parameter ids
_playerInput / _animator / _controller / _input  cached components
_threshold:float  look input dead zone, default 0.01
_hasAnimator:bool  whether an Animator was found
IsCurrentDeviceMouse:bool  true when the active control scheme is "KeyboardMouse"

Methods:
Awake() / OnDestroy()  forward to base
Start()  cache components and animation ids, set the base-class angle limits, reset the timeout timers
Update()  refresh the animator and IsMoving, then ZoomHandle, JumpAndGravity, GroundedCheck, Move
LateUpdate()  CameraRotation while active
AssignAnimationIDs()  hash the animator parameter names into ids
GroundedCheck()  sphere probe with an offset, writes the animator Grounded parameter
CameraRotation()  accumulate and clamp yaw and pitch (mouse not scaled by deltaTime) and drive the Cinemachine target
Move()  compute the target speed with smooth accel/decel and turning, CharacterController.Move, then sync the animator
JumpAndGravity()  jump and gravity; reset timers on the ground, set FreeFall on timeout, clear the jump input while airborne
ClampAngle(value, min, max)  normalise to +-360 degrees and clamp
OnDrawGizmosSelected()  draw the ground probe sphere coloured by the grounded state
OnFootstep(animationEvent) / OnLand(animationEvent)  play audio when the event weight is above 0.5
Teleport(position, rotation, obstacleLayers, checkRadius)  optional safety check, toggle the CharacterController, move the player and sync the camera -> returns success
Transport(Transform target)  -> Teleport with the target pose

Notes:
- Every animator write is guarded by _hasAnimator; removing the guard throws on characters without an Animator.
- TopClamp / BottomClamp come from the base class and must not be redeclared here, or the base fields are shadowed and the 70 / -30 values set in Start are lost.
- The mouse control scheme already delivers frame-rate independent look input, so it must not be multiplied by Time.deltaTime.
- The ground probe centre is shifted down by GroundedOffset and GroundedRadius must match the CharacterController radius, otherwise the probe misfires.
- The CharacterController must be disabled around SetPositionAndRotation during a teleport, otherwise the controller overrides the new position.
- The jump input must be cleared while airborne, otherwise the buffered input triggers an extra jump on landing.
