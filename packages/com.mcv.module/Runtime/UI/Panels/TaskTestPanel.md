# Contract: TaskTestPanel

Role: quiz task panel (View); placeholder. Init is empty and the question data never reaches the UI yet.

Fields:
titleText:Text  title text
questionParent:Transform  parent for the question UI (not built yet)
currentQuestion:QuestionClip  currently selected question
questionList:QuestionData  quiz data, always empty in this placeholder

Methods:
Init(question)  no-op placeholder
SetQuestion(question)  forward to Init
SelectQuestion(data)  store the current question
GetPanelContent()  snapshot text of the question, its options and the correct answers

Notes:
- GetPanelContent dereferences currentQuestion without a null check, so it throws when SelectQuestion was never called.
- This is a placeholder: the real question UI is not assembled yet.
