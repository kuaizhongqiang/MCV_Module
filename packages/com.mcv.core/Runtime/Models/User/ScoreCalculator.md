# Contract: ScoreCalculator

Role: score rules (pure static computation); defines the full-score split, the unit price, the awarding rules and the aggregation.

Fields:
FullScore / StructureCategoryFullScore / MeasureCategoryFullScore / ExamCategoryFullScore  const 100 / 40 / 30 / 30
ExamQuestionCount / DefaultStructureUnitCount / DefaultMeasureUnitCount / DefaultExamUnitCount  const 5 / 8 / 6 / 1
ScoreDecimals / ScoreEpsilon  const 2 / 0.001f

Methods:
IsScoredTask(type)  Structure / Inspection / Exam score, everything else does not
GetCategoryFullScore(type)  category cap, 0 for a non-scoring type
CountEnabledUnits(projectData, type) / CountEnabledUnits(projectData)  enabled unit counts
GetDefaultUnitCount(type)  the fallback denominator (8 / 6 / 1) when configuration is missing
GetUnitFullScore(type, enabledUnitCount, unitIndex)  price = category score / enabled count, the last unit absorbs the rounding remainder
GetExamQuestionFullScore(examUnitFullScore, questionCount)  per-question score
SumCategoryFullScore(...)  category totals check
ScoreUnit(completed, unitFullScore)  binary award (no partial credit)
ScoreExam(correctCount, examUnitFullScore, questionCount)  add one price per correct question
SumClipScore(clip) / SumClipFullScore(clip) / SumTotalScore(data)  clip subtotal, clip cap and the total (the only aggregation口径)
IsUnitFull(task) / IsPerfect(data, expectedUnitCount) / CountCompletedUnits(data) / EvaluateCompletion(data, expectedUnitCount)  completion checks
Apply(data, expectedUnitCount)  the only aggregation entry point: recompute clip subtotals, total and completion
IsClipCompleted(clip) / Round(value)  clip completion (false without scored units) and rounding by ScoreDecimals

Notes:
- Awarding is binary for structure and measurement: finishing the whole chain takes the full price, leaving early scores 0 with no partial credit; the exam adds one price per correct answer.
- Unit price is "category score / enabled count" rounded to 2 decimals, so floating point drift can produce 99.99; hence a full-completion run must short-circuit to FullScore through IsPerfect instead of summing.
- IsPerfect requires expectedUnitCount: with an unknown count it cannot prove nothing was missed and returns false, otherwise a mid-run report would be judged perfect.
- Missing assets are handled by disabling the unit (taskActive = false); that category's score is then redistributed inside the category and the total stays 100.
- Apply is the only aggregation entry point: ClipScore.score and the completion flags are derived there, business code must not write them.
- SumClipScore gates on TaskScore.isCompleted so a leftover score on an unfinished unit counts as 0, which keeps the "finished equals full marks" rule closed.
- The class has no MonoBehaviour, no lifetime and touches neither files nor Unity APIs, so it can be unit tested on its own.
