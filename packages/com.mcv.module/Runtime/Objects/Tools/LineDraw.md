# Contract: LineDraw (+ LineDrawData)

Role: static tube-mesh builder for wires — control points plus parameters become a Mesh on a GameObject; owns per-object mesh lifetime and scratch reuse.

Fields:
LineDraw.s_ScratchVertices / s_ScratchNormals / s_ScratchUv / s_ScratchTriangles / s_ScratchArcLengths  reusable work arrays
LineDraw.s_ScratchDepth:int  nesting depth guard for scratch reuse
LineDraw.s_OwnedMeshes:Dictionary<GameObject,Mesh>  per-object mesh registry
LineDraw.s_DeadEntries:List<KeyValuePair<GameObject,Mesh>>  scratch list for pruning
LineDrawData.width:float  tube diameter
LineDrawData.sectionSegments:int  total path samples
LineDrawData.RadialSegments:int  radial subdivisions, at least 3
LineDrawData.material:Material  renderer material
LineDrawData.bazierOffsetDirection:Vector3  bend direction (nonzero enables it)
LineDrawData.bazierOffsetDistance:float  bend distance

Methods:
GetScratch<T>(canReuse, ref scratch, size)  reuse an exact-size scratch or allocate
CreateLine(name, points, data)  new GameObject(MeshFilter, MeshRenderer) -> RebuildMesh
UpdateLine(lineObj, points, data)  validate -> ensure components -> RebuildMesh -> return lineObj
ValidatePoints(points)  require at least 2 points, else Log.Error
RebuildMesh(go, controlPoints, data)  GeneratePath -> ApplyTubeMesh -> return go
GeneratePath(controlPoints, data)  Catmull-Rom plus sin-squared displacement -> Vector3[] path
CatmullRom(p0, p1, p2, p3, t)  tension 0.5 spline point
ApplyTubeMesh(go, path, data)  depth guard -> ApplyTubeMeshCore
ApplyTubeMeshCore(go, path, data, canReuseScratch)  fill vertices / normals / uv / triangles -> AcquireMesh -> assign mesh
AcquireMesh(go, mf)  reuse the owned mesh, else create one and register it
ReleaseLine(lineObj)  clear the filter and destroy the owned mesh
PruneDestroyed()  drop registry entries whose GameObject is gone
DestroyMesh(mesh)  DestroyImmediate in the editor, Destroy at play time
ComputeTangent(path, index)  central-difference tangent

Notes:
- s_ScratchDepth must be checked before the Core call: a nested rebuild must allocate fresh arrays, otherwise the inner call wipes data the outer call has not yet assigned to its mesh.
- Each GameObject needs its own Mesh: shared meshes (prefab default, Instantiate copies) get overwritten by every line, leaving only the last rebuilt one visible.
- GetScratch requires an exact length match because mesh.vertices is a whole-array assignment; a longer buffer carries stale vertices and triangle indices.
- PruneDestroyed is the fallback sweep for objects destroyed without calling ReleaseLine.
