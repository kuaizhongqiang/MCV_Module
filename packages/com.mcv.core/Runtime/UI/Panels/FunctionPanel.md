# Contract: FunctionPanel

Role: bottom function-button bar (View); builds the buttons from the FunctionBtn prefab, maps each id to an event and animates panel and switch visibility.

Fields:
btnParent:Transform  parent the function buttons are instantiated under (required)
switchBtn:Button  switch that re-opens the panel (must carry a CanvasGroup)
functionBtns:List<Button>  created buttons in creation order
spacingObjs:List<GameObject>  created spacing objects
layoutSpacting:Vector2  HorizontalLayoutGroup spacing when shown (.x) and hidden (.y)
FunctionBtnPrefabName  const UI-package prefab name "FunctionBtn" (see UIPrefabUtil)
SpacingObjPrefabName  const UI-package prefab name "FunctionSpacing" (see UIPrefabUtil)
isActiveNow:bool  initial visible state
m_TargetActive:bool  state the current or starting animation heads for, used to debounce duplicate triggers
m_LayoutGroup:HorizontalLayoutGroup  cached layout group of btnParent
m_SwitchCanvasGroup:CanvasGroup  cached CanvasGroup of switchBtn
m_PanelRect / m_SwitchRect:RectTransform  cached rects of panel and switch
SpacingAfter  static HashSet<string>  ids after which a spacing object is inserted ("BackBtn", "MuteBtn")
DefaultBtnNames  static string[]  default id order Exit / Back / Setting / Mute / ResourcePanel / Summit / Record

Methods:
Awake()  base first, validate btnParent and switchBtn, cache the components, set ignoreParentGroups on the switch, build the buttons, RequestLayoutRebuild(btnParent), apply the initial state
SetFunctionBtnActive(btnName, isActive)  show or hide one button, clearing its listeners when hiding -> RequestLayoutRebuild(btnParent)
CreateFunctionBtns(btnNames)  instantiate every id in order and insert the spacing objects
CreateFunctionBtn(btnName)  instantiate one button prefab and bind its click event
InstantiateBtn(btnName)  load the prefab, name the instance, fill label and icon
CreateSpacingObj()  instantiate the spacing prefab
GetClickEvent(btnName)  id -> event delegate (null for an unknown id)
ButtonLabelText(btnName)  id -> Chinese label ("退出" / "返回" / "设置" / "静音" / "资源" / "提交" / "记录")
GetBtnByName(btnName)  find a created button by name
ButtonEventClean(btn)  remove every click listener of one button
SetLabelText(btn) / SetBtnIco(btn, icoSprite)  write child [1]'s Text / child [0]'s Image sprite; the label write resolves that node's TextComponent on the spot (a runtime instance has no cacheable field) and falls back to TextComponent.SetTextOn only when the node has no component
SetUIActive(isActive)  override; ignore when already at the target, otherwise restart the animation coroutine
SetUIActiveImmediately(isActive)  override; stop the animation and apply the state at once
ActiveState(isActive) / OverrideAnimCoroutine(isActive)  write or lerp panel alpha, switch alpha and layout spacing

Notes:
- Both layout refreshes go through PanelBase.RequestLayoutRebuild (a frame later, depth-descending): the buttons are created at runtime with TextComponent-backed labels, so a same-frame ForceRebuildLayoutImmediate measures the not-yet-assembled text.
- The switch must animate exactly opposite to the panel (panel shown -> switch alpha 0, panel hidden -> switch alpha 1); both visible or both hidden leaves no way to re-open the bar.
- m_SwitchCanvasGroup.ignoreParentGroups must stay true, otherwise hiding the panel hides the switch too and it cannot be clicked to expand again.
- The already-at-target guard (m_TargetActive) must stay in SetUIActive, or the switch alpha flickers 0-1-0.
- Hiding a button clears its listeners on purpose: a hidden button must not react to clicks.
- The button id is the contract between the prefab name, GetClickEvent and ButtonLabelText; a new id has to be registered in all three.
- Button child order is fixed: [0] = icon Image, [1] = label Text.
- OnFunctionPanelSwitch is declared but never raised.
