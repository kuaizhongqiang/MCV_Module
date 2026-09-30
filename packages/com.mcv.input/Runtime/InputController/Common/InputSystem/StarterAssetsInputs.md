# Contract: StarterAssetsInputs

Role: input value cache; stores move, look, jump and sprint coming from PlayerInput callbacks so controllers can read them.

Fields:
move:Vector2  move axis
look:Vector2  look axis
jump:bool  jump pressed
sprint:bool  sprint pressed
analogMovement:bool  use the analog magnitude instead of a binary value
cursorLocked:bool  desired cursor lock state
cursorInputForLook:bool  only look while the mouse button is held

Methods:
OnMove(InputValue)  -> MoveInput(value)
OnLook(InputValue)  reads the platform mouse button (right on desktop, left on WebGL) -> LookInput(value or zero)
OnJump(InputValue)  -> JumpInput(value.isPressed)
OnSprint(InputValue)  -> SprintInput(value.isPressed)
MoveInput / LookInput / JumpInput / SprintInput  field setters
OnApplicationFocus(bool)  -> SetCursorState(cursorLocked)
SetCursorState(bool)  cursor lock (currently an empty body)

Notes:
- OnLook only forwards look when cursorInputForLook is set and the platform mouse button is held; WebGL uses button 0 because right-click misbehaves there.
- The mouse-button number must be chosen per platform, not hardcoded to 1.
