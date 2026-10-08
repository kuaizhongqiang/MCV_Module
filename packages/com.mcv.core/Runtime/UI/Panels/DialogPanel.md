# Contract: DialogPanel

Role: generic dialog view; content and up to two buttons (confirm / cancel). No title node — the dialog identity is a DialogId, not display text. Display and interaction only.

Fields:
contentText:TextComponent  dialog body node
confirmBtn:Button  confirm button, visibility driven by the request
confirmBtnText:TextComponent  confirm button label node (fallback "确认")
cancelBtn:Button  cancel button, visibility driven by the request
cancelBtnText:TextComponent  cancel button label node (fallback "取消")
m_CurrentId:DialogId  identity of the shown dialog; written by Show and read by DialogController to tag the result event
m_ContentTextComp / m_ConfirmBtnTextComp / m_CancelBtnTextComp:TextComponent  the three label nodes' components, cached in Awake from the serialized fields; every writer goes through the cache first and falls back to the field's own SetText

Methods:
Awake()  base first, cache the three label nodes' TextComponents, then bind confirm and cancel
OnDestroy()  unbind both buttons, clear both events, call base
CurrentId  property; the current dialog identity (DialogId), None before the first Show
Show(request)  remember request.Id, render content / labels from the request through the cached components, apply both button flags, show the panel
Hide() / Hide(onHidden)  hide the panel, optionally invoking onHidden once the collapse animation finished
HandleConfirm() / HandleCancel()  raise OnConfirm / OnCancel

Notes:
- Full chain: EventBus<DialogRequestEvent> -> DialogController -> Show -> user click -> OnConfirm / OnCancel -> DialogController -> EventBus<DialogResultEvent>; the panel holds no business logic.
- Button visibility comes from the request: both, confirm only, or none (a pure message box).
- The three text nodes are typed `TextComponent` (retyped from the legacy `Text` fields), so the cached m_*Comp fields are redundant: both branches of each write reach the same component. The retype means the prefab's old `Text` references had to be re-assigned on the node's TextComponent.
- The onHidden overload exists so the result event is published after the collapse animation; hiding immediately deactivates the panel and breaks the running coroutine.
- The panel is rebuilt together with its Canvas, so no dialog state may be cached here — m_CurrentId only carries the identity of the dialog currently on screen.
- The identity is carried by the panel because DialogController builds the result event after Hide: it reads CurrentId before hiding and never keeps the request itself.
- ⚠️ Known defect (recorded in TODO.md, not fixed here): in Show the two label fallbacks (`else` branch) write into `contentText` instead of `confirmBtnText` / `cancelBtnText`.
