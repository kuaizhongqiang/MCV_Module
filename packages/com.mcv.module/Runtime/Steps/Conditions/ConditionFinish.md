# Contract: ConditionFinish

Role: terminal step of a processing; shows StepUIPanel via usingId, and the whole chain ends after Confirm.

Inherits: ConditionStepPanelBase (panel flow shared with ConditionStart / ConditionUI)

Methods:
Type -> ConditionType.Finish

Notes:
- Chain termination is performed by StepManager AFTER this step's three phases finish (IsFinishStep -> EndChain), not by skipping the phases.
- The old behaviour (detect Finish, skip the phases, publish completion) never let this panel appear; that is why it was changed.
- Missing usingId -> Waiting returns immediately, same as the old version.
