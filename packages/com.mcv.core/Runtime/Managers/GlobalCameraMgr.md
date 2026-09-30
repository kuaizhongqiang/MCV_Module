# Contract: GlobalCameraMgr

Role: owns the single project MainCamera: creates or reuses it, keeps CinemachineBrain/CameraBg in sync, reacts to camera and scene-state events.

Fields:
_cam:Camera  the owned camera
brain:CinemachineBrain  cached from _cam
cameraBg:CameraBg  cached from _cam's children
s_IsCreating:bool  static re-entrancy flag around Instantiate

Methods:
Camera  static get (-> GetCamera) / set (stores a non-null camera)
GetCamera()  static; reuse a live _cam, else destroy self-created MainCamera children, load Resources/MainCamera, instantiate under this manager, cache the parts
DelayInit()  _cam = GetCamera() -> cache brain/cameraBg -> one frame -> subscribe CameraBgChange + CameraBlendChange + SceneStateChange + TaskTypeChange -> isInit
OnDestroy()  unsubscribe the same four
OnCameraBgChange / OnCameraBlendChange / OnSceneStateChange / OnTaskTypeChange  EventBus callbacks
BgChange(isSkybox)  clearFlags = Skybox with Color.clear, else SolidColor with Color.black
BlendChange(isCut, blendTime=1)  brain.m_DefaultBlend = Cut/0 or EaseInOut/blendTime
SetCameraBgActive(active)  re-fetch cameraBg when stale, then toggle its GameObject

Notes:
- Re-entrancy: during Instantiate the prefab's Awake calls GetCamera() before _cam is assigned; s_IsCreating makes that call return null so the caller waits a frame instead of recursing (stack overflow).
- GetCamera destroys only cameras parented to this manager (its own past instances); external / UI cameras in the scene are never touched.
- SceneState.Roaming and TaskType.Inspection both hide the camera-background blocker.
- cameraBg must be fetched with GetComponentInChildren<CameraBg>(true) (includeInactive): the bg child is deactivated during Inspection, so the default overload returns null and would wipe the cached reference, leaving the blocker permanently hidden after leaving Inspection.
- BlendChange assumes brain is non-null; it is filled in DelayInit or by GetCamera.
