# Contract: ConditionLineConnect

Role: completes when every template line in step.Lines has a matching connected line; order independent. Reuses the resident line system (ElementManagerBase state machine).

Methods:
Type -> ConditionType.LineConnect
OnPrepare()  CancelDrawing on the manager; hide the template elements
Waiting()  needs ElementManagerBase.Instance -> show template elements -> ShowAnimationsAtFirstFrame -> poll AllLinesConnected -> hide template elements
OnCompleteHide()  hide template elements
FastComplete()  show every line template first (visual "already connected"), then base
AllLinesConnected(mgr)  per template: first and last point of PointList must match some live line
ShowLineElements() / HideLineElements()  wrappers over SetLineElementsActive
SetLineElementsActive(active)  toggle the owning ElementObjBase of each template point, de-duplicated

Notes:
- step.Lines holds inactive ElementLineObj templates whose PointList is pre-filled with the two endpoints.
- Leaving by jump only cancels the in-progress line; already committed resident lines survive, so jumping back still counts as connected (state continuity).
- Missing ElementManagerBase -> warn and skip.
