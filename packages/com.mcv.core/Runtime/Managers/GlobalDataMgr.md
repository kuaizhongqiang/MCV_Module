# Contract: GlobalDataMgr

Role: holds every global data set (system / menu / project / user / language / question / step content / score); the single read-write point for the current clip, current task type, project state, render quality, UI language and text form; loads the JSON files on init and owns the score archive lifecycle.

Fields:
systemData:SystemData  [SerializeField] project info + render quality + languageType (the UI language) + textType (the text form)
menuData:MenuData  [SerializeField] menu (catalogue) tree
projectData:ProjectData  [SerializeField] clips / currentClip / currentTaskType / projectState
userData:UserData  [SerializeField] name / type / password / student id / class / login times
languageData:LanguageData  [SerializeField]
questionData:QuestionData  [SerializeField]
stepContentData:StepContentData  [SerializeField] step text by id
scoreData:ScoreData  [SerializeField] runtime archive, not a saved field
runStartTime:DateTime  [NonSerialized] start of this run, for elapsed-time accounting
SystemData / MenuData / ProjectData / UserData / LanguageData / QuestionData / StepContentData / ScoreData  get/set wrappers around the fields
ScoreDirectory:string  static; StreamingAssets/Score

Methods:
DelayInit()  async load SystemData -> LanguageData -> MenuData -> ProjectData -> QuestionData -> StepContentData, apply the render quality, then isInit = true
TryGetClip(key) / PickClipText(clip)  the only copy of the text fallback chain: current language -> clips[0] (Chinese) -> null
GetLanguageType() / SetLanguage(type)  read SystemData.languageType (Chinese when the data is not ready) / write it and save; no broadcast and no refresh, because the language is a cold-start setting
GetProjectClip() / GetProjectClip(clipId)  current clip, or a clip by id (null when absent)
GetCurrentTaskType() / SetCurrentTaskType(type)  read / write the single source ProjectData.currentTaskType
GetProjectClipIndex()  index of the current clip inside clips; -1 when unset or absent
SetCurrentClip(clip)  write ProjectData.currentClip (the only writer)
GetProjectState() / SetProjectState(state)  read / write projectState (the only writer; synced from SceneState before the canvas rebuild)
GetTaskData(type) / GetTaskData(clipId, type)  task data through the current or a named clip; null when absent
VerifyLogin(userName, password, type)  login check; currently always true (whitelist not implemented)
SetUserData(userName, password, type) / SetUserData(userName, indentyNum, className, password, type)  overwrite UserData and record the login time
GetRenderQuality() / IsRenderQualitySetted()  the quality level (High when data is not ready) / whether it was ever set
GetTextType()  the text form from SystemData.textType, Legacy when the data is not ready; TextComponent reads it to pick its Legacy/TMP assembly path
ApplyRenderQuality(level)  push the level to QualitySettings only, never writes data
SetRenderQuality(level)  apply + write the level and qualitySetted + save; only from a user click
SaveSystemData()  write SystemData back to StreamingAssets/Data/SystemData.json
GetScorePath(fileName)  {student id | Anonymous}.json
BeginScoreSession()  load the archive and restart the elapsed timer
PreviewScore(expectedUnitCount = -1)  settle elapsed time -> recompute totals -> save -> return the archive; <= 0 means compute from ProjectData
ReportScoredUnit(clipId, clipName, taskId, taskName, taskType, completed, correctCount = -1)  price the unit, ensure clip/task score, Submit, ScoreCalculator.Apply; non-scoring types -> null
ResetScore()  clear the in-memory archive and restart the timer (keeps the file on disk)
LoadScoreData(user) / SaveScoreData(archive)  read / overwrite StreamingAssets/Score/<archive>.json; WebGL stays in memory
CountRecordedUnits(archive, taskType)  private; counted units of that category, used to place the rounding remainder
GetMenuData() / GetProjectName() / GetRootMenus() / GetChildMenus(parent)  menu and project-name reads
EnsureArchive(self) / NewArchive(self) / GetProjectName(self)  private archive helpers
WriteJson()  private, #if UNITY_EDITOR; write SystemData/ProjectData/UserData/LanguageData back to JSON

Notes:
- currentClip, currentTaskType and projectState each have exactly one writer (SetCurrentClip / SetCurrentTaskType / SetProjectState). Writing those fields directly on ProjectData bypasses the convention; consumers that need the current task must subscribe TaskTypeChangeEventData or call GetCurrentTaskType rather than keeping their own copy.
- Task data never sits on ProjectData: always read it through GetTaskData(type) or GetTaskData(clipId, type); both return null when absent.
- SetUserData: the 3-argument overload passes null for student id/class, the 5-argument one keeps the current values for empty strings; lastLoginTime must be written before loginTime is updated, otherwise the previous login time is lost (0001-01-01 means no history).
- The score file is named after the student id (user.ScoreFileName) and falls back to Anonymous. ScoreData is a Unity-serialized field, so its constructor values are overwritten by deserialization; EnsureArchive therefore re-applies the empty identity on the non-new path, or the JSON ends up with blank id / softwareName / student fields.
- PreviewScore advances runStartTime, so the same elapsed time is never counted twice; completion state is decided only in PreviewScore / Recalculate, while ReportScoredUnit's ScoreCalculator.Apply refreshes device subtotals and the total.
- ReportScoredUnit prices a unit by category split (structure 40 / measurement 30 / exam 30) with CountEnabledUnits as the divisor, falling back to the baseline unit counts (structure 8 / measurement 6 / exam 1). Callers must not compute scores themselves.
- DelayInit must load LanguageData, otherwise WriteJson overwrites the JSON with defaults and editor-generated clips are wiped; MenuData and ProjectData must be loaded before the AI warm-up, which needs the current menu tree and the current content description; the method must end with isInit = true or the Setup chain waits 15s.
- Render-quality loop: always apply the JSON level on start whatever qualitySetted says; write only after the user actually clicks a level (writing on panel creation would mark it set and the panel would never appear again); skip re-applying the same level because SetQualityLevel reloads render-pipeline assets.
- Language is a cold-start setting: GetLanguageType reads SystemData.languageType (moved out of LanguageData, which now holds only the clip table); TextComponent reads it once in Awake, so there is no LanguageChangedEvent and no runtime hot switch. SetLanguage writes and saves only — it deliberately does not broadcast, and it has no UI caller yet.
- The text form is a cold-start setting on the same footing: GetTextType reads SystemData.textType and falls back to Legacy when SystemData is not ready, so the read is null-safe in the same way as GetRenderQuality. There is no setter and no broadcast — the form is read once while TextComponent assembles and is never hot-switched, which is why the read is a plain static facade rather than a setter + event pair.
- WebGL has no local file system: all score reads/writes live under #if !UNITY_WEBGL and stay in memory; the synchronous WriteJson is editor-only.
