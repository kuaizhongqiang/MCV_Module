# Contract: InspectionProbeObj

Role: inspection probe (red / black test lead); dragging moves it in a plane through the tip, a contact happens when the detection area meets an inspection point and publishes contact and snap events, and a drag that ends without contact returns to the last contact position.

Fields:
probeType:InspectionProbeType  [SerializeField] red / black, sent with the contact event
probePoint:Transform  [SerializeField] the tip; with several simultaneous hits the nearest wins; falls back to its own position
areaCollider:Collider  [SerializeField] the long detection area along the camera view; empty = no contact detection
movePlaneNormal:MovePlaneNormal  [SerializeField] drag plane normal; Auto (default) = the world axis closest to the view direction, CameraFacing = strictly screen-parallel
resetDuration:float  [SerializeField] reset transition length in seconds
snapScaleFactor:float  [SerializeField] visual scale factor while snapped (relative to the base)
snapScaleDuration:float  [SerializeField] seconds to reach the snapped scale
m_HomePos:Vector3  initial position (reset target before any contact)
m_ReturnPos:Vector3  reset target, updated on every successful contact
m_MovePlane:Plane / m_PlaneReady:bool  drag plane (rebuilt on every press) and its ready flag
m_VisualRoot:Transform / m_VisualBaseScale:Vector3 / m_Scale:float  visual layer, its base scale, current factor (1 = base)
m_Dragging:bool / m_GrabOffset:Vector3  dragging flag and the position-minus-plane-point offset captured on press
m_ContactPoint:InspectionElementPointObj  current contact point (null = none)
m_ResetCoroutine:Coroutine  reset transition
m_Snapped:bool / m_SnapPoint:InspectionElementPointObj  snapped state and the snapped point (needed for the release event)
m_Candidates / m_OverlapHits / m_Touched / m_WarnedNoCandidates  candidate cache, overlap buffer, per-frame touched points, warning de-duplication

Methods:
Awake()  cache home and return positions, resolve the visual layer, warn when areaCollider is unset
Update()  while dragging follow the mouse and watch for release, then UpdateContact -> UpdateSnap -> UpdateScale
OnDisable() / OnDestroy()  ClearState -> clear the state and publish the contact and snap releases
MoDownEvent()  rebuild the plane, set m_Dragging, record m_GrabOffset
MoMoveEvent(delta)  same-frame follow only (delta is not used)
MoUpEvent()  end the drag -> EndDrag
FollowMouse()  position = plane point + grab offset -> Physics.SyncTransforms
EndDrag() / CheckMouseReleased()  stop dragging and reset when not in contact / left-button safety net
UpdateContact()  resolve the current contact point and publish on state flips
FindTouchedPoint()  nearest registered point that either overlaps or lies inside the area
CollectOverlappedPoints() / OverlapArea(buffer) / IsInsideArea(position)  overlap hits back to points / BoxCollider overlap query / ClosestPoint test
PublishContact(point, isContact) / PublishSnap(point, isSnapped)  publish the contact / snap event
IsSnapped  property; in contact and not dragging
UpdateSnap()  publish on snap flips
InitVisualRoot()  resolve root -> child 0 -> child 1, record the base scale, verify the area is not inside the visual layer
UpdateScale() / ResetScale() / ApplyScale()  snapped scaling and restore
RefreshMovePlane() / ResolvePlaneNormal(viewForward) / TryGetMousePlanePoint(out point)  plane rebuild, normal mode, mouse-plane intersection
StartReset() / ResetCoroutine() / StopReset() / ClearState()  reset transition and state clearing
TipPosition / AreaUsable  tip position and area availability

Notes:
- This component must sit on the same GameObject as the cursor pick collider, and the detection area is best kept on the root too: GlobalInteractiveMgr only takes the first Physics.Raycast hit and then GetComponent<InteractiveBase>(), otherwise the probe cannot be picked up and the hover state is cleared.
- Contact is not resolved through physics callbacks: registered points are scanned every frame and only the nearest to the tip is kept.
- Follow and reset both need Physics.SyncTransforms (the project disables autoSyncTransforms), otherwise fast drags are a frame late.
- Scaling only affects the visual layer; putting the detection area under it would make the geometry flip during the scaling animation.
- The hierarchy is fixed as root -> child 0 -> child 1; changing it requires updating InitVisualRoot.
- Highlight used to be pushed into the contacted point while dragging; that path was removed with the highlight service, and the contact / snap events are now the only output.
