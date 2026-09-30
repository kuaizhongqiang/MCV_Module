# Contract: ResultSummitController

Role: score-preview controller; turns ResultSummitPanel's buttons (close / submit) into actions.

Fields:
SubmitDialogId  const DialogId.SubmitScore; claims the submit confirmation

Methods:
Awake()  base -> permanently subscribe DialogResultEvent (unsubscribe then subscribe)
OnViewBound()  unsubscribe then subscribe View.OnSubmitClicked / View.OnCloseClicked
OnDestroy()  unbind the two buttons and unsubscribe DialogResultEvent -> base
OnSubmit()  publish DialogRequestEvent(SubmitDialogId, ...)
OnDialogResult(result)  claim by DialogId; confirmed -> SubmitScore
SubmitScore()  log only; the upload hook goes here
OnClose()  View.Hide() — hide only, no scene change, no destroy

Notes:
- The panel is created and shown by the opener (MenuController.OnResultClick), not here: a lazily created panel's Start/Bind runs a frame later, so the opener still holds an empty View, hence "who opens it calls Show".
- Data prep (PreviewScore settle + JSON write + ScoreRecordFormatter.Build) also lives in the opener.
- Results are claimed by DialogId and controllers persist across canvases, so this id must differ from the other publishers.
- SubmitScore is a log-only stub by decision: the score JSON is already written when the preview opens; replace this one place when upload is wired in.
