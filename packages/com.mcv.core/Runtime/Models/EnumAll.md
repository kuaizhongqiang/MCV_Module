# Contract: EnumAll

Role: the single home of every global enum; supplies the values used by JSON serialization and the inspector, grouped by #region (Rendering / Input / UI / Audio / Scene / User / Element / Interactive / Task / Animation / Language / Text).

Fields:
PlayMode  Debug | Release
RenderQualityLevel  Low | Medium | High
MouseMoveState  Moving | Idle
ComponentType  Text | Video
TextType  Legacy = 0 (default) | TMP = 1; the text form selected by SystemData.textType, consumed by TextComponent
VideoType  Legacy（单值：框架只用 Unity 原生播放器）
OverrideAlignment  UpperLeft ... LowerRight | Justified | Auto
DialogId  None (plain hint, nobody claims the result) | Exit | Back | BackToMenu | QuitApp | EnterProject | SubmitScore | BackFromExam; one value per publisher, because resident controllers would otherwise claim the same result
AudioSouceType  BGM | Speaker | Effect
AudioEffectType  Click | Dragging | Success | Fail | Hover | None
SceneState  Setup | Start | Login | Menu | UI | Roaming
UserType  Unknow | Student | Teacher | Admin
CompletionStatus  Unfinished = 0 | Completed = 1
InspectionProbeType  Red | Black
MultimeterGearType  Off | Resistance | VoltageDC | VoltageAC | CurrentDC | CurrentAC | Diode | Continuity; decides which CheckPointData field is read
TerminalPairKind  NoContact | NcContact | Other
ElementActuationState  Normal | Actuated
ElementType  None ... Line
ElementPointNameType  None ... NinetySix; InspectorName carries the terminal silkscreen (1L1 / 3L2 / 13NO / 21NC / A1 / 接地 / 95 ...)
MovePlaneNormal  Auto | CameraFacing | X | Y | Z
SwitchGesture  Press | Toggle | Drag
TaskType  None | Purpose | Equipment | Principle | LineConnection | Training | Test | Info | Structure | Inspection | Exam
QuestionType  None | SingleChoice | MultipleChoice | TrueFalse | FillInBlank
QuestionUsage  Exam (default) | Step
ProjectState  Start | Menu | Roaming | UI | Exam
ConditionType  Default | Click | Drag | Tool | UI | Question | LineConnect | Finish | Start | MeasurePair | GearAdjust
StepStutus  Ready | Waiting | Complete
StepContentType  None | UI | Tips
ObjAxis  X | Y | Z
LanguageType  Chinese | English
TextFinishLayer  Write = 0 | Typography = 1 | Layout = 2; how far a TextComponent has settled (Write = form fixed + text assigned, Typography = plus CJK line-break settling, Layout = plus the panel rebuild), consumed by TextComponent.OnFinished and PanelBase.WaitAllTextFinished
AssemblePhase  None | Assembling | Ready | Failed; TextComponent's assembly stage — the only judge for the ready gate and for re-assembly (None means "before the data gate, do not read the form yet")
PendingKind  None | Literal | Key | Clip; what a buffered write before assembly means, so a SetText(LanguageClip) is not lost

Methods:
(none)

Notes:
- [InspectorName("中文")] is a semantic string: changing a character loses the inspector display, so it must stay byte-identical.
- The enum value is part of the JSON contract in several places (TaskType, QuestionUsage, StepContentType, ConditionType); adding a value usually requires updating the matching switch and the converter as well.
- ConditionType's inline Chinese comments were moved into this document; the Interactive group sits before the Task group.
