# Contract: InspectionLineObj

Role: inspection line object; builds a mesh line from a point list plus draw parameters, once in Awake and on demand from Update.

Fields:
points:List<Transform>  the points the line runs through
data:LineDrawData  draw parameters
meshFilter:MeshFilter / meshRenderer:MeshRenderer  cached components
lastStateKey:int  snapshot of the last build
hasStateKey:bool  whether a snapshot exists

Methods:
Awake()  base -> cache components -> CreateLine
Update()  rebuild only when the data is legal and the snapshot changed
OnDestroy()  LineDraw.ReleaseLine -> base
CreateLine()  public; update the snapshot and LineDraw.UpdateLine
BuildStateKey()  allocation-free integer hash of point count + world positions + own transform + all draw parameters
LineDataLegal()  at least 2 points, width > 0, RadialSegments > 3

Notes:
- The own transform must be part of the hash: vertices are local while points are world space, so moving the object without a rebuild makes the line drift.
- ReleaseLine is mandatory on destroy, otherwise the mesh registry keeps a stale entry.
