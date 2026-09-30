# Contract: ConditionDrag

Role: completes when dragObj is dragged onto targetObj and released over it; a miss restores the object and lets the student retry.

Methods:
Type -> ConditionType.Drag
OnPrepare()  hide dragObj and targetObj
Waiting()  loop: wait for Down on dragObj (global interaction event) -> hide dragObj (picked up) -> wait for left-button release -> RaycastHitTarget(targetObj) decides success; on miss re-show dragObj and retry
OnCompleteHide()  hide dragObj and targetObj

Notes:
- Press is detected through the global Down event; release is polled with IsMouseUp(), NOT an Up event, because releasing over empty space publishes only a Click with Target = null.
- Any blocking point checks IsForceCompleted so NextStep / Skip can interrupt.
- Missing dragObj or targetObj -> warn and skip.
