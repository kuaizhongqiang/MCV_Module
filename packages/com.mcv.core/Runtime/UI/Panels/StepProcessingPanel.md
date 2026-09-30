# Contract: StepProcessingPanel

Role: step-processing panel (View); builds the step's button row from the UI-package prefab StepProcessingBtn (see UIPrefabUtil) and drives its show/hide animation.

Fields:
btnParent:Transform  parent hosting the buttons and spacers (required)
showText:Text  text label, only null-checked in Awake
m_ShowTextComp:TextComponent  the label node's component, cached in Awake; the Awake guard treats "component present" as configured, because in TMP form the node's legacy Text is unloaded (disabled then Destroy; it must be unloaded, as Unity rejects a second Graphic on the same GameObject) and the field becomes a fake null
buttons:List<Button>  spawned buttons
spacings:List<GameObject>  spacers between buttons
ButtonPrefabName  const "StepProcessingBtn"
SpacingPrefabName  const "StepProcessingSpacing"
m_LayoutGroup:HorizontalLayoutGroup  layout of btnParent; its spacing is animated
isActiveNow:bool  initial visible state
m_TargetActive:bool  last requested state, used to debounce duplicate SetUIActive
spacingHide / spacingShow / parentShow / parentHide:int  spacing values for the hidden and shown states
currentBtn:Button  the currently highlighted button

Methods:
Init(btnNames)  clear old buttons -> spawn one button per name with a spacer between -> highlight the first
currentBtnName()  name of the current button
SetButtonActive(btnName)  forward to SetButtonCurrentState
CreateButtons(btnNames) / CreateButton(name) / CreateSpacing()  instantiate the button and spacer prefabs; CreateButton resolves the spawned label node's TextComponent on the spot (a runtime instance has no cacheable field) and writes through it, falling back to TextComponent.SetTextOn
SetSpacingLayoutSpacing(obj, spacing) / SetAllSpacingsLayoutSpacing(spacing)  set and rebuild one / all spacers
SetButtonCurrentState(btnName)  set currentBtn and refresh every button's highlight
SetButtonShow(btn, isCurrent)  styling placeholder, empty body
SetUIActive(isActive)  override; debounce then start the animation
SetUIActiveImmediately(isActive)  override; stop the animation and apply instantly
ActiveState(isActive)  set alpha / interactable and the target spacings
OverrideAnimCoroutine(isActive)  lerp alpha and spacing

Notes:
- The m_TargetActive debounce is required: without it, calling SetUIActive with the already-current value restarts the coroutine and flickers alpha 0-1-0.
- OverrideAnimCoroutine reads spacings[0], so a panel with a single button (no spacer) throws inside the coroutine.
- Awake needs btnParent and showText assigned, otherwise it logs an error and returns without building anything; showText counts as assigned when either the field or m_ShowTextComp is present.
- Button and spacer prefabs come from the UI package (UIPrefabUtil.Get), so a missing prefab yields a null entry in the lists rather than an exception at build time.
