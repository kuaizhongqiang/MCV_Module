# Contract: InputControllerBase

Role: shared base of every input controller; centralises the camera pitch clamp and the mouse-wheel FOV zoom, and registers itself with GlobalInputMgr.

Fields:
isActive:bool  [SerializeField] whether this controller is active, default true
TopClamp / BottomClamp:float  [SerializeField] camera pitch limits in degrees, default 89 / -89
mouseSensitive:float  [SerializeField] mouse sensitivity, default 1
defaultFov:float  [SerializeField] default field of view in degrees, default 42
zoomMin / zoomMax:float  [SerializeField] usable FOV range, default 20 / 120
zoomSpeed:float  [SerializeField] wheel zoom speed, default 5
startPos:Vector3 / startRot:Quaternion  [SerializeField] captured start pose
mainCamera:Camera  cached main camera, resolved by DelayInit
zoomHandleCoroutine:Coroutine  coroutine handle shared by zooming and restoring
IsActive:bool  public accessor for isActive
IsMoving:bool  set true by subclasses while moving, used to trigger the automatic FOV restore
_hasDefaultFov:bool  whether the default FOV has been cached

Methods:
Transport(Transform target)  abstract; subclasses move the controller to the target
Awake()  starts the DelayInit coroutine
Update()  runs only while isActive
OnDestroy()  unregisters from GlobalInputMgr
DelayInit()  polls until GlobalInputMgr is ready, registers itself and caches the main camera
ZoomHandle()  read the wheel; while moving call TryRestoreDefaultFov and return, otherwise clamp and adjust the virtual camera FOV with inertia
ZoomHandleDelay(vcam, targetFov)  interpolate the virtual camera FOV to the target over 0.15s
TryRestoreDefaultFov()  ease the virtual camera FOV back to the cached defaultFov
ZoomRestoreCoroutine(vcam)  interpolate the virtual camera FOV back to defaultFov over 0.15s
TryGetVirtualCamera(vcam)  take the active virtual camera from the CinemachineBrain, caching the default FOV on the first success

Notes:
- defaultFov is cached only on the first successful virtual-camera lookup (_hasDefaultFov); a zoom arriving before the camera is ready is ignored, so changing the caching point shifts the restore baseline.
- Zooming and restoring share zoomHandleCoroutine and stop each other: two separate handles would let both write the same virtual camera FOV concurrently.
- IsMoving gates the automatic FOV restore; removing that branch makes the wheel zoom jitter while the player is moving.
