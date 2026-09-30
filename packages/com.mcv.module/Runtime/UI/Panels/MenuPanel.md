# Contract: MenuPanel

Role: view of the menu page; hosts the device entry buttons plus the roaming / quit / result entries and raises click events.

Fields:
btnsParent:Transform  parent of the device entry buttons
btnIds:string[]  JSON ids per button; also drives which entries show
roamingBtn:Button  roaming entry (always present)
quitBtn:Button  quit entry
resultBtn:Button  result preview entry
companyImage:GameObject  company object toggled by SetCopyright
copyrightText:GameObject  copyright object toggled by SetCopyright
breathImages:List<Image>  images driven by the breathing-light coroutine while the panel is shown
currentClip:MenuClip  currently selected menu clip
menuBtns:List<Button>  created and validated menu buttons
menuClips:List<MenuClip>  menu clips parallel to menuBtns
animToggleName:string  readonly "active"; the Animator's Bool parameter that drives show/hide
anim:Animator  the Animator on this (root) object, resolved in Awake; controller = MenuPanelActive
m_IsShowing:bool  the tracked show state; the GameObject stays active under the Animator path, so this is the only source of truth
IsShowing:bool  public read of m_IsShowing for summoners (do not read gameObject.activeSelf)

Methods:
Awake()  apply button visibility, bind the entries, call base, resolve the Animator, then seed m_IsShowing from the Animator Bool
OnDestroy()  call base
Init()  placeholder for controller assembly (currently empty)
SetCopyright(ifCopyright, ifCompany)  toggle the company and copyright objects
SetUIActive(isActive, onHidden)  override; records m_IsShowing, then returns early when there is no Animator; writes the Animator Bool ("active") instead of the base fade coroutine -- AnyState -> MenuPanel_Show (0.25s) / MenuPanel_Hide (0.1s); on show it also starts BreathLightenAnim(breathImages, 2f), which is never stopped on hide
BindBtns()  bind roaming / quit / result clicks to the events
SetBtnsActive()  show the buttons that exist in menu data and bind their clicks; returns early when the JSON is not ready
OnMenuBtnClicked(index)  refresh the selection and raise OnMenuBtnClick
SetBtnsSelected(clip)  mark exactly one button selected
SetBtnSelected(btn, isSelected)  write the selected style: btnsParent child [1]'s Image color = white when selected, clear otherwise

Notes:
- Button order and ids are tied to btnIds and the btnsParent child order; keep them aligned.
- The JSON drives entry visibility: a missing clip hides that button.
- The roaming button is resident with fixed "enter roaming" semantics; returning from the roaming popup goes through the device buttons.
- Click bindings die with the instance, so no unsubscribe is needed.
- Show/hide is Animator-driven (MenuPanelActive on the root): Bool `active`, AnyState -> MenuPanel_Show / MenuPanel_Hide, both clips 0.5s and both with an empty m_Events. The parameter defaults to false, so the Animator enters the Hide state as soon as the panel is instantiated -- a caller that needs a visible panel has to call SetUIActive(true) (MenuCanvas.OnRebuild does exactly that).
- SetUIActive currently only writes the Bool: `onHidden` is never invoked, no completion is detected and the panel is never deactivated, so the resting alpha / interactable comes from whatever the clip leaves behind (the base coroutine no longer runs). Until the clips are authored, this means the menu is not left visible: MenuPanel_Show and MenuPanel_Hide are byte-identical and both end with the root CanvasGroup at Alpha 0 / Interactable 0.
- Show/hide state lives here (`m_IsShowing` / `IsShowing`): the panel is instantiated active and SetUIActive never deactivates it, so `gameObject.activeSelf` stays true forever. RoamingFunctionController must read `IsShowing` for its summon/hide toggle (reading activeSelf made the back button hide-only after the first summon).
