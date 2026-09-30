# Contract: DialogEventDispatcher

Role: central dispatcher for DialogRequestEvent; any system that publishes a dialog request goes through one fixed resolution chain that shows the dialog.

Fields:
s_Initialized:bool (static)  idempotency guard, true while subscribed to DialogRequestEvent

Methods:
Initialize()  static; subscribes OnDialogRequested, idempotent
Shutdown()  static; unsubscribes OnDialogRequested, call it on scene switch or app quit
IsInitialized  static bool property
OnDialogRequested(request)  static; GlobalUIMgr -> active Canvas -> GetPanel<DialogPanel>() -> Show(request)

Notes:
- The subscription chain is fixed as GlobalUIMgr -> currently active Canvas -> GetPanel<DialogPanel> (lazily created when missing) -> Show(request); never shortcut a step.
- Every failing step degrades safely and returns instead of throwing: GlobalUIMgr not ready, no active Canvas, or panel creation failure (the last logs an error and expects the UI bundle entry ui_DialogPanel to be present, i.e. MCV Build/UI prefab AB has run and Setup preloaded the UI package).
- s_Initialized makes Initialize and Shutdown idempotent: call Initialize once at framework start and Shutdown at scene switch or singleton teardown, otherwise the static handler leaks across reloads.
- Responsibility split: this class only shows the dialog, while DialogController still listens to DialogPanel.OnConfirm / OnCancel and publishes DialogResultEvent.
