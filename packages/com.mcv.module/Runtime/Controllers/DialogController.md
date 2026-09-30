# Contract: DialogController

Role: dialog coordinator; subscribes DialogPanel confirm and cancel and publishes DialogResultEvent once the hide animation has completed.

Methods:
OnViewBound()  clear then add OnConfirm / OnCancel
OnDestroy()  unsubscribe OnConfirm / OnCancel (View null-guarded)
HandleConfirm()  capture View.CurrentId -> View.Hide(() => publish DialogResultEvent(id, true))
HandleCancel()  capture View.CurrentId -> View.Hide(() => publish DialogResultEvent(id, false))
GetDialogId()  View.CurrentId, or DialogId.None when the View is gone

Notes:
- Showing is not this controller's job: DialogRequestEvent is resolved by DialogEventDispatcher through GlobalUIMgr -> active canvas -> GetPanel<DialogPanel>, so it must not subscribe to that trigger.
- Publish must happen inside Hide's callback: publishing first lets the consumer (e.g. a scene-state switch) deactivate the panel early and StartCoroutine throws.
- The identity is captured before Hide so it is read while the view is still alive; the claim key is a DialogId enum, not display text.
