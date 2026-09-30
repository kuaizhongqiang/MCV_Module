# Contract: ElementManagerBase

Role: resident element/line registry + the hand-drawn line state machine; the project-side instance is InspectionElementManager.

Fields:
instance:ElementManagerBase  static singleton, bound in Awake by the parent (parents Awake before children, so child self-registration finds it)
elements:Dictionary<string, ElementObjBase>  id -> element
lines:Dictionary<string, ElementLineObj>  id -> committed line
state:DrawState  Idle | Drawing
startPoint:ElementPointObj  line start while drawing
tmpLine:GameObject  the temporary line object
planeDistance:float  [SerializeField] distance of the virtual plane from the camera; <=0 falls back to camera->startPoint
lineDrawData:LineDrawData  [SerializeField] parameters of the temporary line
lastTmpEnd / hasTmpEnd  last temporary end point, used to skip redundant mesh rebuilds
TmpEndEpsilonSqr:float  const 1e-8; squared movement threshold

Methods:
Instance  static get/set
GetElement<EL>(id) / GetLine<LI>(id)  static; null when absent
Awake()  virtual; instance = this
DelayInit()  one frame -> subscribe GlobalInteractionEventData -> isInit
OnDestroy()  virtual; unsubscribe
Update()  while Drawing: end = hovered valid point, else PlaneProject(); rebuild the temp line only past the threshold
RegisterElement(element) / UnregisterElement(element)  mutate this instance's dictionary, not the static field
RegisterLine(line) / UnregisterLine(line)  same for lines
SetPlaneDistance(distance)  override the plane distance from task data
CancelDrawing()  destroy the temp line and reset the state machine to Idle
GetLines()  IEnumerable<ElementLineObj> of committed lines; polled by ConditionLineConnect
OnGlobalInteraction(data)  Click only: Idle -> clicking a point starts drawing; Drawing -> a valid different point commits, anything else cancels
GetTmpLineData()  manager lineDrawData, or the start point's own data when unconfigured
PlaneProject()  ray vs the camera-facing virtual plane; falls back to the camera-start distance
DrawState  [enum] Idle, Drawing

Notes:
- Registration targets this instance's dictionaries on purpose: callers reach the manager through GetComponentInParent, which may differ from the static instance.
- Element registration needs a non-null Data with an id; duplicates are ignored.
- CancelDrawing removes only the in-progress line; committed lines stay, so step state survives a jump.
- Nothing runs unless this is the static instance and isInit: Update and OnGlobalInteraction both bail out otherwise.
