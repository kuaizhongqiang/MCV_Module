# Contract: UserData (+ StudentInfo / ScoreData / ClipScore / TaskScore)

Role: user identity of the logged-in session; derives the score file name and display name and provides an identity snapshot.

Fields:
userName / indentyNum / className / password:string  name, student number, class and password
userType:UserType  user type, default Unknow
loginTime / lastLoginTime:DateTime  this and the previous login time
ScoreData.id / displayName:string  student number (or Anonymous) and name
ClipScore.score / isCompleted / fullScore  derived read-only values of a clip subtotal
TaskScore.score / isCompleted / fullScore  derived read-only values of a scored unit

Methods:
UserData() / UserData(userName, indentyNum, password, className, userType)  default and logged-in constructors (empty values fall back to Empty)
ScoreFileName / ScoreDisplayName  file name (student number or Anonymous) / display name (falls back to 游客)
ToStudentInfo()  identity snapshot without the password
EnsureIdentity(user, softwareName)  fill empty identity fields only, never overwrite existing values
GetClipScore(clipId) / EnsureClipScore(clipId, clipName, fullScore)  lookup and create a clip record
GetTaskScore(taskId) / EnsureTaskScore(taskId, taskName, taskType, fullScore)  lookup and create a unit record
AccumulateRun(runDuration)  accumulate duration and run count, refresh endTime (startTime keeps the first value)
Recalculate(expectedUnitCount)  forward to ScoreCalculator.Apply, the single aggregation rule
Submit(score, completed)  overwrite with the latest round, completion only ever increases, attempts + 1

Notes:
- The score file is named after the student number and overwrites: the same student's repeated runs merge instead of creating files.
- StudentInfo deliberately does not reuse UserData: the score file lands on disk and may be uploaded, so it must not carry the password.
- ClipScore / TaskScore deliberately store only id plus display name and do not embed ProjectClip, so the score file stays free of the question bank, steps and package configuration.
- ScoreData.Recalculate is the single aggregation rule; scores and completion are derived, never written by business code.
- UserData.json under StreamingAssets/Data is read-only after a build, so the runtime keeps the identity in memory only.
