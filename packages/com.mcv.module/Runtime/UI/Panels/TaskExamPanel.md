# Contract: TaskExamPanel

Role: exam panel (View); display and input only, never judges. The question flow is driven by TaskExamController.

Fields:
companyText:GameObject  company object toggled by SetCopyright, by GlobalUIMgr.IfCompany (required)
copyrightText:GameObject  copyright object toggled by SetCopyright, by GlobalUIMgr.IfCopyright (required)
contentText:Text  question text (required)
optionsParent:Transform  parent for the option rows (required)
submitBtn:Button  submit button (required)
backBtn:Button  back entry, returns to the menu page (required)
tipsText:Text  result / no-selection hint (required)
rightTipsColor / wrongTipsColor:Color  hint colors
rightAudio / wrongAudio:AudioEffectType  result sounds
RightTips / WrongTips / NoSelectedTips  const hint texts
options:List<TaskExamOptionsToggle>  option rows of the current question
selectedIndex:int  selected option index (-1 = none)
m_TipsTextComp:TextComponent  the hint node's component, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy; it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so the hint color must go through the component to reach the TMP that is actually rendered
m_ContentTextComp:TextComponent  the question node's component, cached in Awake for the same reason; after the swap the Text field is a fake null and TextComponent.SetTextOn silently no-ops

Methods:
Awake()  base first, cache the hint and question nodes' TextComponents, then validate the seven references (a Text field only counts as missing when its cached component is null too) and bind the submit and back buttons
ShowQuestion(clip)  clear options -> reset hints and selection -> write the question through m_ContentTextComp (falls back to TextComponent.SetTextOn) -> build rows -> RequestLayoutRebuild()
ShowResult(isRight)  write hint text and color, play the result sound
SetSubmitInteractable(interactable)  enable or disable the submit button
SetCopyright(ifCopyright, ifCompany)  toggle the company and copyright objects (the panel does not read the flags itself)
ShowFinish()  empty on purpose; the real finish presentation is still TODO
SubmitClick()  no selection -> local hint; else raise OnSubmit
BackClick()  raise OnBackClick (the panel never switches pages itself)
SelectOption(index, isOn)  single-choice exclusivity (re-clicking the same option deselects)
ClearOptions()  dispose and clear every option row
SetTips(text, color) / ClearTips()  write / clear the hint through m_TipsTextComp.SetText and .ColorValue (each falls back to TextComponent.SetTextOn / SetColorOn only when the node has no component)

Nested type TaskExamOptionsToggle  one option row (Toggle + index letter + text); UI-package prefab name "ExamOptionsToggle" (see UIPrefabUtil), hierarchy root(Toggle) -> [0]Marks -> [1]CountText and [1]OptionText; its contentTextComp / indexTextComp:TextComponent hold both label nodes' components, resolved on the spot in CreateToggle because a runtime instance has no cacheable field
Methods:
TaskExamOptionsToggle(itemText, index, parent, onValueChanged)  create the row
CreateToggle(itemText, index, parent)  load the prefab, resolve both label nodes' TextComponents and write the labels through them (each falls back to TextComponent.SetTextOn), bind the Toggle listener to the callback
OnToggleChanged(isOn)  forward to the callback
SetOnWithoutNotify(isOn)  set the state without firing the callback
Dispose()  remove the listener and destroy the row
GetIndexChar(index)  (char)('A' + index)

Notes:
- Layout rebuild is shared now: the private coroutine plus its LayoutEntry collector moved into UILayoutRebuilder and is reached through PanelBase.RequestLayoutRebuild (one frame later, depth-descending, skipped while inactive).
- The panel shows a chain of questions: order, judging, the delay before the next question and the end of the exam all live in TaskExamController.
- OnSubmit (int) and OnBackClick (no args) are the two events the controller subscribes to in OnViewBound.
- Copyright follows the same contract as ContentFunctionPanel / RoamingFunctionPanel: the controller pushes GlobalUIMgr.IfCopyright / IfCompany into SetCopyright on every rebind; the panel never reads those flags.
- ClearOptions / Dispose are mandatory before a new question, otherwise rows accumulate and Toggle listeners leak into destroyed objects.
- ShowFinish is deliberately empty; do not resurrect the old "clear options plus end text" behaviour without filling in the real presentation.
- GetIndexChar overflows past 'Z' when there are more than 26 options.
- The panel holds display state only: correctness is kept in the controller's question data.
