# Contract: ElementLineObjEditor (+ ElementLineAutoRefresh)

Role: inspector for ElementLineObj plus a global editor-time auto refresh that rebuilds line meshes whenever their state changes.

Fields:
ElementLineAutoRefresh  [InitializeOnLoad] static class holding the per-line snapshot dictionary

Methods:
OnInspectorGUI()  manual generate and clear buttons
ElementLineAutoRefresh.OnUpdate()  compare the state snapshot of every ElementLineObj in the open scenes and rebuild the changed ones
Snapshot(line)  state key: isStatic, transform, draw parameters, point count and every point's world position
EnsureCollider(line)  add a MeshCollider to existing scene lines (RequireComponent does not backfill)

Notes:
- The refresh is independent of the selection, so dragging an intermediate point updates the mesh live.
- The snapshot dictionary is pruned of destroyed lines each tick so it does not grow without bound.
- A rebuild is triggered only when the snapshot key changes, which keeps the editor quiet while idle.
- Existing scene lines need the MeshCollider backfilled explicitly, because RequireComponent only applies to newly added objects.
