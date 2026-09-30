# Contract: QuestionData (+ QuestionClip)

Role: question bank root; holds the QuestionClip list while the constructor only fills metadata.

Fields:
questions:List<QuestionClip>  the question list, not populated by the constructor
QuestionClip.usage:QuestionUsage  Exam (default, the exam draw pool) or Step (step question, taken by id)
QuestionClip.options:List<...>  the options, also not populated by the constructor

Methods:
QuestionData()  initialise id / displayName / description

Notes:
- Neither constructor fills default entries: Newtonsoft appends to an already initialised collection instead of replacing it, so a default entry would be duplicated on every JSON round trip.
- Exam and Step questions share one bank but two pools: the exam only draws usage = Exam, step conditions take their question by id.
