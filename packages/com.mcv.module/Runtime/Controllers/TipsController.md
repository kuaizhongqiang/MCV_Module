# Contract: TipsController

Role: tip-bar scheduler between step data and the tip panel; turns StepPreparedEvent into "step tip / operation tip", caches content across panel rebuilds and owns the auto-hide timers.

Fields:
m_StepPartOpen:bool  step-tip section expanded; true by default
m_OpPartOpen:bool  operation-tip section expanded; false by default (the user expands it)
m_StepAutoHidden / m_OpAutoHidden:bool  that side was closed by the auto-hide timer, not by the user; new content reopens it
m_HasContent:bool  any side has content, used to refill a rebuilt panel
m_StepText / m_StepImageKey:string  cached step-tip text and image key
m_OpText / m_OpImageKey:string  cached operation-tip text and image key
m_StepTimer / m_OpTimer:Coroutine  running auto-hide timers

Methods:
Awake()  base -> subscribe StepPreparedEvent (resident controller, once)
OnDestroy()  unsubscribe -> StopTimer both -> UnbindView -> base
OnViewBound()  clear then add both toggle handlers -> refill cached content + ApplyState -> restart timers
UnbindView()  detach both toggle handlers
OnStepPrepared(e)  FindTips(Step.TipsId) ? SetStepTips + SetOpTips : SetStepTips(Description) + SetOpTips(null)
FindTips(tipsId)  scan StepContentData.contents for a StepTipsData whose id matches
SetStepTips(text, imageKey=null) / SetOpTips(...)  public; ApplySide(true / false)
SetTips(stepText, opText)  set both sides at once
ApplySide(isStep, text, imageKey)  cache -> ReopenIfAutoHidden -> PushToView; empty both -> clear that side only
ReopenIfAutoHidden(isStep)  when that side was auto-hidden: expand it and restart its timer
HasAnyContent()  any of the four cached fields non-empty
PushToView(isStep)  open -> SetContentWithImage (animated), closed -> SetContentImmediate (silent)
OnStepPartToggled / OnOpPartToggled(isOpen)  user voice: set open, clear the auto-hidden flag, start or stop the timer
StartTimer(isStep) / StopTimer(isStep)  AutoHideDelay > 0 and IsAutoHideAllowed -> run / stop the side's coroutine
IsAutoHideAllowed()  false for LineConnection and Training (tips stay visible)
GetCurrentTaskType()  GlobalDataMgr.GetCurrentTaskType()
AutoHideAfter(delay, isStep)  wait -> if still open, close it and set auto-hidden

Notes:
- The panel is rebuilt with the canvas (View only displays); open state, auto-hide timing and the "should auto-hide" decision all live in the controller and are refilled on rebind.
- Auto-hidden and user-closed must be told apart: only after auto-hide does new content reopen the side, otherwise only the first step's tip ever shows.
- AutoHideDelay comes from the panel; <= 0 disables auto-hide.
- Tip content prefers the StepContentData entry pointed to by StepHandler.tipsId (both sides given); when absent it falls back to the step description and the operation tip is cleared.
- SetStepTips: a null or empty text clears that side; imageKey is a GlobalAddressableMgr package id, preferred over text and falling back on load failure.
