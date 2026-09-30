# Contract: ConditionStepPanelBase

Role: shared base for "show StepUIPanel -> wait for Confirm -> close panel". Used by ConditionStart / ConditionUI / ConditionFinish; they differ only in Type and in which StepUiData entry step.UsingId points to.

Methods:
ResolvePanel()  static; GlobalControllerMgr.Find("StepUIController") as IStepUiPanel; null when missing
OnPrepare()  override; close any leftover panel (jump-back, or an interrupted previous step)
Waiting()  override; ShowAnimationsAtFirstFrame -> resolve panel -> subscribe OnPanelClosed -> ShowData(step.UsingId) -> WaitUntilOrForceComplete -> unsubscribe -> ClosePanel

Notes:
- Content comes from StreamingAssets/Data/StepContentData.json, entries with contentType = UI (StepUiData): title + pages; multi-page shows paging buttons.
- Degradation: missing controller or unresolvable content id -> warn and complete immediately, never hang the chain.
- MUST subscribe before ShowData: the controller fires "closed" synchronously when the id has no content, so showing first would lose that callback.
