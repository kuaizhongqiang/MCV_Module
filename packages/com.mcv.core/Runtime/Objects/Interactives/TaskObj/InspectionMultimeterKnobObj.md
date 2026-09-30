# Contract: InspectionMultimeterKnobObj

Role: gear knob of the digital multimeter; drag to rotate with the cursor, switching gear as the pointer passes a detent and snapping to the nearest gear on release, with hard stops at both ends and SetGear / NextGear / PreviousGear for direct selection.

Fields:
GearSetting  [Serializable] gearType:MultimeterGearType (Off shows no reading), range:float (upper limit, 0 = no over-range check; unit follows the gear type), angle:float (knob angle in degrees around rotateAxis)
gears:List<GearSetting>  [SerializeField] the gear table in panel order; entry 0 is the initial gear, at least one entry
startIndex:int  [SerializeField] initial gear index (clamped when out of range)
knobVisual:Transform  [SerializeField] the layer that actually turns; empty = this transform
rotateAxis:Vector3  [SerializeField] rotation axis in the visual layer's local space; the panel knob uses Z, default Vector3.forward
zeroEuler:Vector3  [SerializeField] local euler of the visual layer at angle 0 (the authored rest pose)
rotateDuration:float  [SerializeField] seconds for the release snap and programmatic changes; <= 0 instant
wrap:bool  [SerializeField] whether NextGear / PreviousGear wrap around (programmatic entries only; dragging never leaves the two ends)
dragDeadRadius:float  [SerializeField] pixel dead zone at the centre; the effective value is min(this, half the screen radius)
invertDrag:bool  [SerializeField] manual fallback to invert the drag direction, default false
m_Index:int  current gear index (-1 = uninitialised)
m_AppliedAngle / m_TargetAngle:float  applied and target rotation in degrees
m_RotateSpeed:float  snap angular speed in degrees per second
m_Visual:Transform / m_VisualRenderer:Renderer  resolved visual layer and its renderer
m_WarnedNoCamera:bool  whether the missing-camera warning was already logged
m_Dragging / m_DragCenter / m_DragScreenRadius / m_DragDeadZone / m_LastMouseAngle / m_DragDelta / m_DragStartAngle / m_DragSign / m_DragMinAngle / m_DragMaxAngle  drag state (screen centre and radius cached on press, accumulated angular delta, direction sign, hard stops)
MinDeadRadius  const 4f, the lower bound of the centre dead zone in pixels

Methods:
GearCount / CurrentIndex / CurrentGear / CurrentGearType / CurrentRange / CurrentGearLabel  gear table views (an empty table reports Off)
GearChanged:event Action<int>  raised on every gear change
Visual / ComposeRotation(angleDegrees) / ResolveAxis()  visual layer, pose built from zeroEuler plus the rotation, normalised axis
Awake()  resolve the visual layer and renderer, clamp the initial gear, apply the pose
Update()  while dragging follow the cursor, otherwise ease toward the target angle, then write the pose
OnDisable()  clear the drag state
MoDownEvent / MoUpEvent / MoMoveEvent(delta)  begin drag, end drag and snap, same-frame follow
BeginDrag()  cache the screen centre, start angle, hard stops and direction sign; log the reject reasons
EndDrag() / UpdateDrag()  snap to the nearest gear -> GearChanged / accumulate the angular delta, clamp and switch gears by passing a detent
CheckMouseReleased()  end the drag when the left button is up
SetAppliedAngle(angle) / ApplyRotation() / StartRotate(targetAngle)  place the angle directly / write the pose / derive the angular speed from the duration
SyncIndexToNearestGear() / FindNearestGearIndex(angle)  switch and log when the index changed / nearest gear by Mathf.DeltaAngle
ResolveDragLimits(referenceAngle, out min, out max)  hard stops relative to the reference angle
MeasureAngleScreenDirection() / MeasureScreenRadius(centerScreen) / ResolveDragSign()  measure the screen-turn sign, the on-screen radius and the final drag sign (measured value plus invertDrag)
ResolveWorldAxis() / TryGetRimDirection(axisWorld, cam, out direction) / GetWorldRadius() / ResolveRenderer()  knob axis, a rim direction inside the screen plane, the world radius from the renderer bounds
TryProject(cam, world, out screen) / TryScreenAngleOf(cam, world, screenCenter, out angle) / ResolveCamera()  world to screen, screen angle around the centre, current camera (GlobalCameraMgr else Camera.main)
GetDragCenterWorld() / TryGetDragCenter(out screenPos)  drag centre from the renderer bounds, in world and screen space
NextGear() / PreviousGear() / SetGear(index)  direct gear selection (clamped; the same gear does nothing) -> GearChanged
ResolveIndex() / UnitOf(gear) / GetDragCenterWorld()  clamped valid index / physical unit string (Ohm, V, A)
EditorGetGear(index) / EditorSetGearAngle(index, angle) / EditorSetGears(list) / EditorSetStartIndex(index)  editor-side calibration helpers

Notes:
- The component must sit on the same GameObject as the pick collider, otherwise the raycast hit has no component and the press lands on nothing.
- The table plus zeroEuler is the only source of orientation: an angle authored in the prefab is overwritten at runtime. This knob turns around Z, hence the Vector3.forward default.
- The drag centre comes from the renderer bounds centre (model pivots often sit outside the circle) and is cached on press, never recomputed per frame, otherwise the centre drifts and the angle delta self-oscillates.
- The angular delta is accumulated per frame instead of "current angle minus start angle", so a single drag past 180 degrees is not shortcut by DeltaAngle; the limits are brought into the same turn with DeltaAngle.
- The direction sign is measured by projection, because inferring it from the axis dot view direction comes out inverted (dragging right rotates left); an unavailable camera falls back to +1 with a warning and invertDrag is the manual fallback.
- The dead zone is min(dragDeadRadius, half the screen radius), since a fixed pixel dead zone swallows a small on-screen knob entirely.
- MoMove and MoUp only fire while the ray hits this object, so continuous following runs in Update and release is covered by CheckMouseReleased.
- The reading is not computed here: InspectionMultimeterObj takes CurrentGear to decide which quantity to read and whether it is out of range. The gear table is assumed to span less than 360 degrees.
