# Contract: ConditionQuestion

Role: shows the question panel for usingId (question id) and completes when the question is answered correctly.

Methods:
Type -> ConditionType.Question
ResolvePanel()  GlobalControllerMgr.Find("StepQuestionController") as IStepQuestionPanel; null when missing
OnPrepare()  close any leftover panel (covers the jump-back case)
Waiting()  ShowAnimationsAtFirstFrame -> resolve panel -> subscribe OnQuestionCorrect -> ShowQuestion(step.UsingId) -> WaitUntilOrForceComplete -> unsubscribe -> ClosePanel
OnCompleteHide()  close panel

Notes:
- Question content lives in the global QuestionData.json, entries with usage = Step, fetched by id (usingId).
- Missing panel -> warn and skip; the chain never hangs.
