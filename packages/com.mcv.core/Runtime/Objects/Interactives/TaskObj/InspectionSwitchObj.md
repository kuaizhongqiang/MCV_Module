# Contract: InspectionSwitchObj

Role: clickable sub-part of a component (test button, operating handle) that slides along one local axis between the up and down limits; handles Press / Toggle / Drag gestures and reports the pressed state.

Fields:
moveObj:Transform  [SerializeField] moved part; empty = this object
moveAxis:ObjAxis  [SerializeField] local axis, default Z
upLimit:float  [SerializeField] released local coordinate
downLimit:float  [SerializeField] pressed local coordinate, default 1
moveSpeed:float  [SerializeField] local units per second, <= 0 instant, default 0.5
gesture:SwitchGesture  [SerializeField] default Press
startPressed:bool  [SerializeField] initial state, default false
dragDeadZone:float  [SerializeField] drag dead zone in pixels, default 3
m_Visual:Transform  resolved move layer (Awake)
m_Applied / m_Target:float  applied and target local coordinate
m_Pressed:bool  pressed state
m_Dragging / m_DragStartPos / m_DragStartMouse / m_DragScreenDir / m_DragUnitsPerPixel  drag state
IsPressed:bool  property -> m_Pressed
Visual:Transform  property -> moveObj ?? transform

Methods:
Awake()  resolve m_Visual, warn on misconfiguration -> set the start state
Update()  dragging -> UpdateDrag + CheckMouseReleased, else MoveTowards m_Applied -> m_Target
OnDisable()  clear m_Dragging
MoDownEvent / MoUpEvent  Drag -> BeginDrag / EndDrag, Press -> SetPressed
MoClickEvent  Toggle
MoMoveEvent(delta)  same-frame drag response
SetPressed(pressed)  no-op when unchanged -> StartMove -> PressedChanged -> log
Toggle()  SetPressed(!m_Pressed)
SetPressedImmediate(pressed)  snap to the position -> PressedChanged on change
PositionOf(pressed)  downLimit / upLimit
StartMove(target) / ApplyPosition() / AxisVector(axis)  set the target (snap when speed <= 0) / write one local position component / local unit vector
ResolveDragLimits(min, max) / PressedOfPosition(position)  limits of the two ends / nearer end
BeginDrag()  calibrate the screen direction and units-per-pixel; reject when unprojectable
UpdateDrag()  dead zone -> clamp -> SetPressedFromDrag -> ApplyPosition
EndDrag() / SetPressedFromDrag(pressed)  snap to the nearer end / flip the state without event spam
CheckMouseReleased()  EndDrag when the left button is up
TryProject(cam, world, out screen) / ResolveCamera()  world to screen (false behind the camera) / GlobalCameraMgr.Camera else Camera.main
EditorCaptureLimit(pressed)  editor-only; record the current pose as a limit

Notes:
- The component must sit on the same GameObject as the pick collider: GlobalInteractiveMgr resolves it from the raycast hit, otherwise clicks land on nothing.
- Visual falls back to the object's own transform when moveObj is empty, so move and drag always have a target.
- MoMove only fires while the ray hits this object, so dragging keeps following in Update (same pattern as the probe and the knob).
- Press requires holding the button, which one mouse cannot do while moving a probe, so teaching setups prefer Toggle.
- BeginDrag refuses when the axis faces the camera, because the drag has no screen-space component to map onto.
