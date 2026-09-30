# Contract: ConditionTool

Role: completes when the tool identified by usingId is dragged from the tool panel onto targetObj and released over it.

Methods:
Type -> ConditionType.Tool
ResolvePanel()  GlobalControllerMgr.Find("StepToolPanelController") as IStepToolPanel; null when missing
OnPrepare()  hide targetObj; clear tool dragging and close the panel
Waiting()  ShowAnimationsAtFirstFrame -> resolve panel -> subscribe OnToolPressed -> ShowPanel -> loop: wait for OnToolPressed(usingId) -> SetToolDragging(usingId) -> wait for release -> SetToolDragging(null) -> RaycastHitTarget(targetObj)
OnCompleteHide()  hide targetObj; close the panel

Notes:
- The panel contract IStepToolPanel is NOT implemented yet, so Tool steps always warn and skip.
- Release is polled with IsMouseUp(), not an Up event (same reason as ConditionDrag).
- Missing panel -> warn and skip; the chain never hangs.
