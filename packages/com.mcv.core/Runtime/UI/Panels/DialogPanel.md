# Contract: DialogPanel

Role: generic dialog view; content and up to two buttons (confirm / cancel). No title node — the dialog identity is a DialogId, not display text. Display and interaction only.

Fields:
contentText:Text  dialog body
confirmBtn:Button  confirm button, visibility driven by the request
confirmBtnText:Text  confirm button label (fallback "确认")
cancelBtn:Button  cancel button, visibility driven by the request
cancelBtnText:Text  cancel button label (fallback "取消")
m_CurrentId:DialogId  identity of the shown dialog; written by Show and read by DialogController to tag the result event
m_ContentTextComp / m_ConfirmBtnTextComp / m_CancelBtnTextComp:TextComponent  the three text nodes' components, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy; it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so the Text fields become fake nulls and the static entry points silently no-op (reads return an empty string) — reads and writes must go through the components

Methods:
Awake()  base first, cache the three text nodes' TextComponents, then bind confirm and cancel
OnDestroy()  unbind both buttons, clear both events, call base
CurrentId  property; the current dialog identity (DialogId), None before the first Show
Show(request)  remember request.Id, render content / labels from the request through the cached components, apply both button flags, show the panel
Hide() / Hide(onHidden)  hide the panel, optionally invoking onHidden once the collapse animation finished
HandleConfirm() / HandleCancel()  raise OnConfirm / OnCancel

Notes:
- Full chain: EventBus<DialogRequestEvent> -> DialogController -> Show -> user click -> OnConfirm / OnCancel -> DialogController -> EventBus<DialogResultEvent>; the panel holds no business logic.
- Button visibility comes from the request: both, confirm only, or none (a pure message box).
- The onHidden overload exists so the result event is published after the collapse animation; hiding immediately deactivates the panel and breaks the running coroutine.
- The panel is rebuilt together with its Canvas, so no dialog state may be cached here — m_CurrentId only carries the identity of the dialog currently on screen.
- The identity is carried by the panel because DialogController builds the result event after Hide: it reads CurrentId before hiding and never keeps the request itself.
