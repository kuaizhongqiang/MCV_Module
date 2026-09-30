# Contract: ConditionStart

Role: start marker of a processing chain; shows StepUIPanel via usingId and completes after Confirm.

Inherits: ConditionStepPanelBase (panel flow shared with ConditionUI / ConditionFinish)

Methods:
Type -> ConditionType.Start

Notes:
- Semantically "the first step".
- Missing usingId or unresolvable content completes immediately, i.e. no interaction at all.
