# Contract: RenderQualityPanel

Role: render quality view; three level buttons (Low / Medium / High) plus a hardware info text. View only.

Fields:
low / medium / high:Button  the three level buttons
infoText:TextComponent  hardware info and suggestion text
m_InfoTextComp:TextComponent  the info node's component, cached in Awake from the serialized field; after the `Text` -> `TextComponent` retype the cache is a defensive duplicate, and SetInfoText falls back to the field's own SetText
quality:RenderQualityLevel  currently highlighted level (display only)
OnQualitySelected:event Action<RenderQualityLevel>  raised on click; RenderQualityController decides whether the level takes effect
Quality:RenderQualityLevel  property; the currently highlighted level

Methods:
Awake()  base first (canvasGroup is initialised there), cache the info node's TextComponent, then validate the references (a text node counts as configured when either the field or its cached component is set) and bind the three buttons; logs and returns when a reference is missing
OnDestroy()  base, then unbind the three buttons
InitHardwareInfo()  fill the info text and pre-highlight the suggested level
SetBtnSelected(level)  refresh the highlight only, no event
SetInfoText(text)  set the info text through m_InfoTextComp (falling back to the field's SetText)
GetSuggestionQuality()  static; suggest a level by VRAM (needs D3D11 / OpenGLCore; <= 2048MB Low, <= 6144MB Medium, else High)
GetQualityDisplayName(level)  static; the Chinese level name; it feeds the controller's Log only (Logs stay Chinese on purpose), so panel copy must NOT reuse it
QualityStepName(level)  static; the localised level name for panel copy, read through Lang.Get (ui.quality.name.low / medium / high)
OnLowClick() / OnMediumClick() / OnHighClick()  select a level
Select(level)  highlight, then raise OnQualitySelected
BuildHardwareInfo(suggest)  system / CPU / memory / GPU / VRAM info with the suggestion line; every line goes through Lang.Get (ui.quality.hw.*), otherwise the English build would show this block in Chinese forever
SetSelected(btn, isSelected)  toggle the mark object at btn -> BG -> Frame -> Color; warns when missing

Notes:
- Must not read or write SystemData and must not touch QualitySettings; RenderQualityController decides whether a level takes effect.
- Click equals apply: the prefab has no confirm button, so Select both highlights and raises the event.
- The mark path is tied to the BG fragment hierarchy, so changing that prefab requires updating SetSelected.
- base.Awake must run first: canvasGroup is initialised there, and the controller still calls SetUIActive when references are missing.
