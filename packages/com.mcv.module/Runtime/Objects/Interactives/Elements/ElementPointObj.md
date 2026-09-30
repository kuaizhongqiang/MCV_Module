# Contract: ElementPointObj

Role: wiring terminal (point); draws the drag preview, updates the temporary line, and turns a hovered point into a permanent ElementLineObj.

Fields:
pointType:ElementPointNameType  [SerializeField] default None; feeds the terminal symbol in the name
lineData:LineDrawData  [SerializeField] shared by preview and final line, defaulted when unset
tmpLine:GameObject  runtime only; the in-progress drag line
Type  ElementType.Point

Methods:
Awake()  base only
DelayInit()  wait until the parent ElementObjBase.isInit -> name = GetName(), data.id = name -> isInit = true
CreateTmpLine() / CreateTmpLine(LineDrawData)  create the drag line at this point (both ends at self; the overload destroys the old one first)
UpdateTmpLine(line)  follow the hovered point (collapse back to self when none or self)
UpdateTmpLine(line, Vector3 to) / UpdateTmpLine(line, Vector3 to, LineDrawData)  update to an explicit end point
CreateLine() / CreateLine(ElementPointObj target)  spawn an ElementLineObj under the manager and draw it -> DestroyLine(); null or self target cancels
DestroyLine()  destroy tmpLine (play-mode aware)
GetHoverPoint()  GlobalInteractiveMgr.Instance.Current as ElementPointObj
GetDrawData()  lineData, or a default thin line when width / sectionSegments are unset
GetName()  parent element name + "_" + ElementNameMap.GetName(pointType)
MoClickEvent  deliberately empty (reserved for the controller-driven wiring path)

Notes:
- The terminal name is the wiring semantic and is also what line names are built from (Element_{a}_{b}).
- The default draw data only exists so a zero-width (invisible) line is never created; do not remove it.
- MoClickEvent must stay empty: the wiring interaction loop is owned by the controller / LineConnection mode.
