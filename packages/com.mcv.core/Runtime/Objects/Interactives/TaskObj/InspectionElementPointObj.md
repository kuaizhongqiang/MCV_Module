# Contract: InspectionElementPointObj

Role: inspection point; reports which component and point it belongs to and builds the point name. Highlight is not part of the framework: a host subscribes to MoEnter / MoExit if it wants hover presentation.

Fields:
element:ElementType  [SerializeField] owning component type (Chinese name source for the point name)
point:ElementPointNameType  [SerializeField] the point's role on that component (kept for scene authoring)
pointName:string  the display name (component Chinese name + object name), captured in Awake

Methods:
Awake()  base -> build pointName
GetPointName()  the point name; the subscriber of the probe contact event reads it

Notes:
- The point name is captured once in Awake, so renaming gameObject.name afterwards will not follow.
- The probe contact highlight (hover + contact merged) was removed with the highlight service; InspectProbeObj no longer calls into this class.
