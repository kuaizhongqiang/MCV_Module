# Contract: ConditionClick

Role: completes when the configured targetObj is clicked.

Methods:
Type -> ConditionType.Click
OnPrepare()  hide targetObj before Waiting
Waiting()  activate targetObj, subscribe GlobalInteractionEventData, match Type == Click && Target == targetObj
OnCompleteHide()  hide targetObj

Notes:
- Missing targetObj -> warn and skip; the chain never hangs.
- The interaction handler is unsubscribed before Waiting returns.
- Hiding targetObj in Prepare is intended: "the target is invisible before the step starts" is by design, not a bug.
