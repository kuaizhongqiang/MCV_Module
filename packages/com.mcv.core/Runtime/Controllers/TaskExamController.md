# Contract: TaskExamController

Role: exam controller; draws questions from the bank, shuffles question and option order, judges answers, paces the flow and reports the score. All decisions live here, the panel only displays.

Fields:
BackDialogId:DialogId  const DialogId.BackFromExam; the id this page's back confirmation result is claimed by
QuestionCount:int  const 5; questions drawn per exam
QuestionInterval:float  const 2; delay before the next question after a correct answer, seconds
clips:List<QuestionClip>  drawn questions of this run (business state lives in the controller, not the panel)
currentIndex:int  current question index, -1 = not started
nextQuestionCoroutine:Coroutine  pending next-question timer, non-null while a correct answer waits

Methods:
OnInit()  resident subscribe DialogResultEvent (clear then add)
OnViewBound()  re-subscribe View.OnSubmit and View.OnBackClick (clear then add) -> View.SetCopyright(GlobalUIMgr.IfCopyright, GlobalUIMgr.IfCompany) -> draw clips -> show the first question; an empty bank warns and aborts
OnDispose()  stop the pending coroutine -> unsubscribe View.OnSubmit / View.OnBackClick -> unsubscribe DialogResultEvent -> base
OnBackClick()  publish DialogRequestEvent(BackDialogId, BuildBackMessage(), confirm + cancel); does not switch pages itself
BuildBackMessage()  confirm text carrying the current project name, with a generic fallback when ProjectData is not ready
OnDialogResult(result)  ignore unless Confirmed and Id == BackDialogId -> StopNextQuestion + ReturnToMenu (no score report: quitting mid-exam scores nothing)
OnAnswerSubmitted(index)  judge clip.options[index].isCorrect -> View.ShowResult(isRight); wrong keeps the question (re-selectable), right locks submit and starts NextQuestionCoroutine
NextQuestionCoroutine()  wait questionInterval -> advance currentIndex -> show the next question or FinishExam
FinishExam()  ReportExamScore -> View.ShowFinish -> ReturnToMenu
ReportExamScore()  report the Exam scored unit with completed:true -> warn when clip / task data / unit is missing
ReturnToMenu()  publish SceneStateChangeEventData(SceneState.Menu)
DrawQuestions(count)  shuffle the bank -> clone each question and shuffle its options; returns clones, never mutates the read-only bank
TakeBank()  collect usage = Exam questions that have options from GlobalDataMgr.Instance.QuestionData
CloneWithShuffledOptions(source)  copy id / displayName / description / questionText / questionType and the options, then Fisher-Yates the copy
Shuffle<T>(list)  in-place Fisher-Yates over IList<T>

Notes:
- The question bank is read-only data (Models/README.md): every draw clones before shuffling, because shuffling in place pollutes the global bank.
- Scoring is binary: a wrong answer can be re-selected, so "finishing" equals "all correct" and counting correct answers would always yield full marks.
- The unit price is computed by GlobalDataMgr.ReportScoredUnit by category; the caller must not compute the score itself.
- Re-examining in one session overwrites with the latest round (TaskScore.Submit); leaving mid-way records nothing (0, no partial credit).
- The panel is rebuilt with its Canvas, so OnViewBound always clears then re-adds the handler.
- ReturnToMenu only publishes state; GlobalUIMgr owns the Canvas switch.
- The back entry goes through a confirmation dialog, so it needs its own DialogId (BackFromExam): controllers are resident and their subscriptions survive page changes, so reusing BackToMenu would make both ContentFunctionController and this one claim the same result and publish SceneStateChangeEventData twice.
- The pending next-question timer is stopped in OnDialogResult (on confirm), not in OnBackClick, so a cancelled dialog does not disturb the running question.
