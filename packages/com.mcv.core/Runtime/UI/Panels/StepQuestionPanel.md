# Contract: StepQuestionPanel

Role: step question panel (View); shows one question for a step condition and reports the chosen option back. Display and input only, never judges.

Fields:
contentText:Text  question text (required)
optionsParent:Transform  parent for the option rows (required)
submitBtn:Button  submit button (required)
tipsText:Text  result / no-selection hint (required)
rightTipsColor / wrongTipsColor:Color  hint colors
rightAudio / wrongAudio:AudioEffectType  result sounds
RightTips / WrongTips / NoSelectedTips  const hint texts
options:List<TaskExamOptionsToggle>  option rows of the current question
selectedIndex:int  selected option index (-1 = none); pure UI state
m_TipsTextComp:TextComponent  the hint node's component, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy; it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so the hint color must go through the component to reach the TMP that is actually rendered
m_ContentTextComp:TextComponent  the question node's component, cached in Awake for the same reason; after the swap the Text field is a fake null and TextComponent.SetTextOn silently no-ops

Methods:
Awake()  base first, cache the hint and question nodes' TextComponents, then validate the four references (a Text field only counts as missing when its cached component is null too)
ShowQuestion(clip)  clear options -> reset hints and selection -> write the question through m_ContentTextComp (falls back to TextComponent.SetTextOn) -> build option rows -> RequestLayoutRebuild()
ShowResult(isRight)  write hint text and color, play the result sound
SetSubmitInteractable(interactable)  enable or disable the submit button
SubmitClick()  no selection -> local hint; else raise OnSubmit
SelectOption(index, isOn)  single-choice exclusivity (re-clicking the same option deselects)
ClearOptions()  dispose and clear every option row
SetTips(text, color) / ClearTips()  write / clear the hint through m_TipsTextComp.SetText and .ColorValue (each falls back to TextComponent.SetTextOn / SetColorOn only when the node has no component)

Notes:
- The panel serves a step condition (ConditionQuestion) and shows one question at a time, unlike the exam panel's question chain.
- ShowQuestion must call ClearOptions first, otherwise option rows accumulate and Toggle listeners leak into destroyed objects; Dispose per option is mandatory.
- Layout rebuild is shared now: the private coroutine plus its LayoutEntry collector moved into UILayoutRebuilder and is reached through PanelBase.RequestLayoutRebuild (one frame later, depth-descending, skipped while inactive) -- the behaviour is unchanged, the implementation is single-sourced.
- Correctness never lives here: only the selected index crosses the boundary.
- Why the cache is required: in TMP form the node's legacy Text is unloaded, so the `Text`-typed field is a fake null and `tipsText.color = ...` would throw MissingReferenceException; the rendered control is the TMP, and ColorValue routes the write through whatever the component currently owns.
