# Contract: ElementLineObj

Role: a drawn connection (tube mesh) between two terminals; builds and releases its mesh and collider, and matches endpoint pairs for line-completion conditions.

Fields:
pointList:List<ElementPointObj>  [SerializeField] at least 2 points required, else nothing is drawn; exposed as PointList
lineDrawData:LineDrawData  [SerializeField] must be filled or nothing is drawn; exposed as LineDrawData
isStatic:bool  [SerializeField] static lines are drawn on Editor / Start; exposed as IsStatic
meshFilter:MeshFilter  runtime only; cached in Awake
meshCollider:MeshCollider  runtime only; cached in Awake
Type  ElementType.Line

Methods:
Awake()  cache meshFilter / meshCollider, set data.id = name -> base -> parent manager.RegisterLine(this)
DelayInit()  wait for manager.IsInit and for the first / last point isInit -> rename to Line_{first}_{last} -> draw when isStatic -> isInit = true -> RegisterLine(this)
OnDestroy()  LineDraw.ReleaseLine(gameObject) -> base -> ElementManagerBase.Instance.UnregisterLine(this)
EditLinePoint(List<ElementPointObj>)  replace the point list
CreateLine()  world -> local vertices, guarding fewer than 2 points / null point / unset draw data (logs and destroys), then LineDraw.UpdateLine + SyncCollider
DestroyLine()  clear the collider mesh, then LineDraw.ReleaseLine
SyncCollider()  copy meshFilter.sharedMesh onto the MeshCollider
Matches(a, b)  true when the first and last points equal {a, b} in either order
MoClickEvent  deliberately empty
MoClickDoubleEvent  DestroyLine()

Notes:
- The line name is built from the endpoint names and written into data.id; ConditionLineConnect relies on Matches.
- Only the mesh created by LineDraw is released; prefab or shared meshes must stay untouched.
- SyncCollider must run right after every mesh rebuild, or picking and visuals diverge.
- Vertices are local: point world positions must be converted with InverseTransformPoint.
