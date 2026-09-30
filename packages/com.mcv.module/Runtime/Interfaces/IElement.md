# Contract: IElement (+ IElePoint, IEleLine)

Role: interaction contract for circuit elements and their terminals and wires; exposes element data and kind plus the temporary and permanent wire lifecycle.

Fields:
Data:DataBase  element data model
Type:ElementType  element kind

Methods:
CreateTmpLine()  (IElePoint) create the temp drag wire from this terminal -> returns the object to feed UpdateTmpLine
UpdateTmpLine(GameObject line)  (IElePoint) update the temp wire toward the current hover target
CreateLine()  (IElePoint / IEleLine) commit the real wire -> builds mesh and collider; cancels when the target is null or self
DestroyLine()  (IElePoint / IEleLine) destroy the wire -> releases the created object and mesh
EditLinePoint(List<ElementPointObj> points)  (IEleLine) set the wire's terminal points; order is significant (first to last)

Notes:
- Implementers: ElementObjBase (IElement), ElementPointObj (IElePoint), ElementLineObj (IEleLine) under Objects/Interactives/Elements.
- IElePoint drives the drag-from-a-terminal flow: the object returned by CreateTmpLine must be fed to UpdateTmpLine each frame, then committed with CreateLine or discarded with DestroyLine, otherwise the temp object leaks.
- IEleLine uses the first and last entries of the point list for its name and for Matches(): feeding an out-of-order or null list desynchronises the wire, its name and its collider.
