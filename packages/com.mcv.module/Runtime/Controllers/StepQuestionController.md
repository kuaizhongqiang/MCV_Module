# Contract: StepQuestionController (implements IStepQuestionPanel)

Role: step question dispatcher; serves the step condition ConditionQuestion — fills one question by id, judges the chosen option and reports the correct answer.

Fields:
correctDelay:float  [SerializeField] seconds to stay after a correct answer before finishing the step
shuffleOptions:bool  [SerializeField] shuffle the options (false for teaching steps)
currentClip:QuestionClip  current question (already cloned, options may be shuffled)
finishCoroutine:Coroutine  finish timer running after a correct answer

Methods:
OnViewBound()  unsubscribe then subscribe View.OnSubmit (idempotent re-subscribe)
OnDestroy()  stop the finish timer, unbind View.OnSubmit -> base
ShowQuestion(questionId)  BuildClip -> resolve or create the panel -> activate -> panel.ShowQuestion; missing question or panel -> warn and fire OnQuestionCorrect
ClosePanel()  stop timer, clear clip, ResolvePanel(false), unsubscribe, deactivate when active
OnAnswerSubmitted(index)  judge options[index].isCorrect -> panel.ShowResult; wrong -> stay; right -> disable submit and start FinishCoroutine
FinishCoroutine() / StopFinish()  wait correctDelay then fire OnQuestionCorrect / stop and clear it
BuildClip(questionId)  find the question by id in the global bank and clone it; null when missing or optionless
Clone(source) / Shuffle(list)  shallow copy with a fresh options list (shuffled when enabled) / in-place Fisher-Yates
ResolvePanel(createIfMissing)  GetActiveCanvas -> GetPanel (create) or FindPanel (no create)

Notes:
- ResolvePanel(false) must be used on the hide path; creating there would flash a brand-new instance.
- The panel is created on demand and never cached: Start and View binding run a frame later, so the class holds the resolved panel directly and OnViewBound is only an idempotent re-subscribe.
- Wrong answers keep the question so the learner can retry; correct answers disable submit for correctDelay.
- Activate before ShowQuestion: the layout rebuild coroutine is skipped while inactive, so a reused panel keeps stale option sizes.
- Deactivation on an already-inactive GameObject must be guarded, otherwise StartCoroutine throws inside the condition's Prepare coroutine and two steps sharing one panel would break on question 2.
- The question bank is read-only; shuffle only the clone.
- A missing question or panel still fires OnQuestionCorrect, so the step chain never deadlocks.
