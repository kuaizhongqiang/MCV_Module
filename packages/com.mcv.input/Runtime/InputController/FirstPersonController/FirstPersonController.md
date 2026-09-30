# Contract: FirstPersonController

Role: first-person player (CharacterController + PlayerInput); ground-checks, moves, jumps with gravity, rotates the Cinemachine camera target and teleports via Transport.

Fields:
MoveSpeed / SprintSpeed / RotationSpeed / SpeedChangeRate:float  movement and look tuning
JumpHeight / Gravity / JumpTimeout / FallTimeout:float  jump apex, custom gravity, jump interval, free-fall threshold
Grounded:bool  ground-check result (not CharacterController.isGrounded)
GroundedOffset / GroundedRadius:float  ground probe sphere offset and radius
GroundLayers:LayerMask  ground layers
CinemachineCameraTarget:GameObject  camera pitch target
ResetVerticalVelocityOnTeleport / ImmediateGroundedCheck:bool  post-teleport behaviour
_cinemachineTargetPitch / _speed / _rotationVelocity / _verticalVelocity / _terminalVelocity  movement state
_jumpTimeoutDelta / _fallTimeoutDelta  countdown timers
IsCurrentDeviceMouse:bool  true when the active control scheme is keyboard and mouse

Methods:
Awake() / OnDestroy()  forward to base
Start()  cache components, clamp pitch to 90 / -90, reset timers
Update()  set IsMoving -> ZoomHandle, JumpAndGravity, GroundedCheck, Move
LateUpdate()  -> CameraRotation when active
GroundedCheck()  sphere overlap sets Grounded
CameraRotation()  apply look (mouse unscaled, gamepad scaled by deltaTime), clamp pitch -> rotate the player
Move()  target speed, accelerate or decelerate, rotate toward input -> _controller.Move
JumpAndGravity()  jump plus gravity, timeouts, disables jump while airborne
ClampAngle(a, min, max)  wrap to [-360, 360] then clamp
OnDrawGizmosSelected()  draw the ground-check sphere
Teleport(pos, rot, layers, radius)  optional obstacle check, toggle the CharacterController, set position and rotation -> returns success
Transport(Transform)  -> Teleport(target.position, target.rotation, ...)

Notes:
- Mouse look must not be multiplied by Time.deltaTime; IsCurrentDeviceMouse is what enforces that.
- Gravity applies two deltaTime multiplications for linear acceleration and is capped by _terminalVelocity, so a per-frame deltaTime change alters the feel.
- Teleport disables the CharacterController around SetPositionAndRotation to avoid position-update conflicts.
