# Contract: ResultSummitViewData (+ ScoreRecordFormatter)

Role: pre-formatted view data of the score preview panel; the same file holds ScoreRecordFormatter, the single place where display text is produced.

Fields:
RowNum .. RowDuration  const string row names that must equal the LabelTextPrefab instance names
labels:Dictionary<string,string>  row name -> label text
values:Dictionary<string,string>  row name -> value text
RecordText:string  the multi-line "experiment step record" block
ScoreRecordFormatter.TimeFormat  const "yyyy-MM-dd HH:mm:ss"
ScoreRecordFormatter.EmptyRecordText  const "暂无记录", used when there is nothing to list

Methods:
Set(rowName, label, value)  register a row; an empty row name is ignored, null label/value become ""
GetLabel(rowName) / GetValue(rowName)  the stored text, or null when the row was never registered
RowCount  number of registered rows
ScoreRecordFormatter.Build(data, projectName)  map student/software/platform/completion/score/times plus RecordText; null data yields an empty view
FormatCompletion(status) / FormatScore(value) / FormatTime(time) / FormatDuration(span)  display formatting ("已完成"/"未完成", "0.##", TimeFormat, "hh:mm:ss")
FormatRecord(data)  one line per clip via FormatClip joined with '\n'; EmptyRecordText when nothing qualifies
FormatClip(clip)  scored units joined with '，', prefixed with "displayName:" (fallback id), null when the clip has no scored unit
FormatUnit(task)  "name score/fullScore" when completed, else "name 未完成"

Notes:
- Rows are matched by name, not by order: moving rows around in the prefab is safe, renaming them breaks immediately.
- The row-name constants deliberately mirror the LabelTextPrefab instance names; a rename must be done on both sides.
- Formatter output is final: the panel and controller must not re-format or re-translate anything.
- FormatScore uses "0.##" without a culture override, so the decimal separator follows the current culture.
- The record lists only task types passing ScoreCalculator.IsScoredTask; other types silently disappear from the text.
