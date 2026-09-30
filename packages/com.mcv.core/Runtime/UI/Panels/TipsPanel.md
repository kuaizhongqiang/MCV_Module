# Contract: TipsPanel

Role: tips-bar panel (View); two independent tip areas (step tip and operation tip), each a double-buffered container. Display and interaction only, state and timing live in TipsController.

Fields:
StepSerializedKey / StepCloneKey / OpSerializedKey / OpCloneKey  const double-buffer dictionary keys, matching the clone object names
StepTipsContent / OpTipsContent:RectTransform  the serialized (always shown) content objects
StepPartSwitchToggle / OpPartSwitchToggle:Toggle  the side switches
MoveDuration:float  content-swap animation length
ToggleTimerDuration:float  auto-hide delay (<= 0 = never auto-hide)
stepDict / opDict:Dictionary<string, TipsContentUtiliy>  serialized and clone helper per side
stepAnimVersion / opAnimVersion:int  animation version counters; stale callbacks are dropped
m_Ready:bool  all references present and the double buffer ready
AutoHideDelay:float  auto-hide delay read from the prefab

Methods:
SetSideState(isStep, isOpen)  sync one side's switch and visibility without firing events
ApplyState(isStepOpen, isOpOpen)  sync both sides
SetContentImmediate(isStep, text, imageKey="")  write the content now, no animation (image first, text fallback)
SetContent(isStep, text)  write text with the swap animation
SetContentWithImage(isStep, imageKey, text)  image-first write with a text fallback
GetContent(isStep)  current text of one side
GetTipsText()  the visible tip text (operation first, then step); read-only use
AnimSwap(isStep)  swap serialized and clone under a version guard
HideAll()  hide every container
HandleStepToggleChanged / HandleOpToggleChanged  raise the matching event
SetToggleWithoutNotify(toggle, isOn)  set a toggle without firing
GetSerialized(isStep) / GetClone(isStep)  fetch the helper of a side
CleanRichText(richText)  strip TMP layout tags (<space=2em>)

Notes:
- The panel is rebuilt together with its Canvas and stores no cross-state data: switch state, auto-hide timing and content source must live in TipsController.
- Do not fill content by calling this class from business code; go through TipsController.SetStepTips / SetOpTips / SetTips.
- The double buffer lets a new value animate in while the serialized object stays the authoritative display.
- A still-running collapse animation on the serialized object must be stopped, otherwise it drags alpha back to 0.
- imageKey wins over text when non-empty; the text is only the fallback when the load fails.
- The version counters drop stale callbacks from rapid consecutive switches, otherwise an old animation writes back content or position.
