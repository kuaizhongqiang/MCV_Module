# Contract: ContentFunctionController

Role: content-page shell controller; turns the Back entry into a confirmation dialog and, once confirmed, a return to the menu. It no longer touches the AI panel.

Fields:
BackDialogId:DialogId  const DialogId.BackToMenu; the id this page's confirmation result is claimed by (a distinct id is what isolates it from FunctionController's resident subscription)

Methods:
OnInit()  subscribe DialogResultEvent (resident, clear before add)
OnViewBound()  clear then add OnBackBtnClick; View.Init(Localized.Name(GlobalDataMgr.GetProjectClip())); View.SetCopyright(GlobalUIMgr.IfCopyright, GlobalUIMgr.IfCompany)
OnDispose()  unsubscribe OnBackBtnClick and the resident DialogResultEvent
OnBackClick()  publish DialogRequestEvent(BackDialogId, BuildBackMessage(), confirm + cancel)
BuildBackMessage()  project name ? "确定要离开《X》返回菜单界面吗？" : "确定要返回菜单界面吗？"
OnDialogResult(result)  ignore unless Confirmed and Id == BackDialogId -> GoBackToMenu
GoBackToMenu()  publish SceneStateChangeEventData(SceneState.Menu)

Notes:
- The back confirmation is event driven: the click only publishes DialogRequestEvent, and DialogEventDispatcher locates the DialogPanel (active canvas -> GetPanel<DialogPanel>() -> Show); this class never looks the panel up and never sets anything active itself.
- The AI entry is gone from this page: the AI panel opens itself through its own switch button (AiDialogPanel.SetPanelActive / SetPanelActiveImmediately), so OnAiClick and its `GlobalUIMgr.GetPanelOnActiveCanvas<AiDialogPanel>()` call were removed, and ContentFunctionPanel no longer has that event.
- The AI panel is still created by ContentCanvas.OnRebuild only when GlobalAiMgr.IsAiEnabled.
- The controller is persistent, so a resident subscription must live in OnInit and the per-panel subscription in OnViewBound (clear before add); the BackDialogId must stay different from other publishers' ids, otherwise two controllers claim one result.
- Only the state event is published; GlobalUIMgr switches the canvas (never SetActive a canvas here).
