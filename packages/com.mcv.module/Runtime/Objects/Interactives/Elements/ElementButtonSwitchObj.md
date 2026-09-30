# Contract: ElementButtonSwitchObj

Role: push-button switch (SB); press / release edges drive the move animation and publish a state-change event.

Fields:
points:List<ElementPointObj>  runtime only; its terminals, exposed as Points
elementMoveAnimation:ElementMoveAnimation  [SerializeField]; rebuilt in Awake with `this`
IsPressed:bool  read-only; true == pressed (contacts closed) == !elementMoveAnimation.Open
Type  ElementType.ButtonSwitch

Methods:
Awake()  base -> rebuild elementMoveAnimation(this, moveObj, moveAxis, moveLimitation, duration) -> Reset() -> force Open = true (released)
MoDownEvent()  on the released -> pressed edge: Open = false -> publish ElementStateChangeEventData
MoUpEvent()  on the pressed -> released edge: Open = true -> publish ElementStateChangeEventData

Notes:
- Both handlers are edge-triggered; without the guard they would spam the event while held down.
- Awake forces the released state so a flow does not need a click first; do not trust the serialized Open value.
- IsPressed is the inverse of the animation's Open flag (Open == released).
