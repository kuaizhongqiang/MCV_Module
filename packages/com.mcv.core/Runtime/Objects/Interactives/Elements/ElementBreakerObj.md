# Contract: ElementBreakerObj

Role: circuit breaker (QS); a click toggles open / closed through the rotation animation and publishes a state-change event.

Fields:
points:List<ElementPointObj>  runtime only; its terminals, exposed as Points
rotationAnimation:ElementRotationAnimation  [SerializeField]; rebuilt in Awake with `this`
isOpen:bool  [SerializeField] default true; exposed as IsOpen
OpenTag / CloseTag  const "Open" / "Close" animation tags
Type  ElementType.Breaker

Methods:
Awake()  base -> rebuild rotationAnimation(this, old.rotateObj, old.RotationStructs) -> play the next-state tag -> flip isOpen
MoClickEvent()  play the next-state tag -> flip isOpen -> publish ElementStateChangeEventData(this)

Notes:
- The played tag names the state being entered, so Awake plays Close and leaves isOpen == false: the serialized default is effectively ignored.
- rotationAnimation is rebuilt at runtime; the serialized instance only feeds rotateObj and RotationStructs.
- Click must publish ElementStateChangeEventData, otherwise step conditions never advance.
