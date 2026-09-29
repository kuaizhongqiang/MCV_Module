# Contract: LineEditTools

Role: editor menu tool that batch-generates, clears and validates ElementLineObj across the currently loaded scenes.

Methods:
GenerateAllStaticLines()  static lines are generated, non-static lines have their meshes destroyed
DestroyAllLines()  destroy the mesh of every ElementLineObj
ValidateLines()  collect and report the per-line configuration issues
AssignAndGenerateLine()  assign the selected points to the single selected line, generate it and mark it static
ValidateAssignAndGenerateLine()  menu validator: enabled only with exactly one line and at least two points selected
GetSelectedLine() / GetSelectedPoints()  the single selected line, or the selected points in selection order
ValidateOne(line) / FindAllLines()  per-line issues and every line in the loaded scenes

Notes:
- Generated line meshes are runtime only and are not scene-serialized; ElementLineObj.DelayInit rebuilds them at runtime.
- AssignAndGenerateLine creates no new GameObjects; it only assigns and generates.
- The generated line is marked static so it is rebuilt automatically at runtime.
