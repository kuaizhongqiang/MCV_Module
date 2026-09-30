# Contract: Log (+ LogLevel, LogEntry)

Role: the project's single logging outlet; wraps Debug.Log with levels, timestamps, module tags and an optional on-screen overlay (console and GUI dual channel).

Fields:
LogLevel  enum: Info | Success | Warning | Error | Verbose
LogEntry  struct: Level, LevelTag, Time, Tag, Message
VerboseEnabled:bool  master switch for Verbose output, default true
TimestampEnabled:bool  console timestamp prefix, default true
RichTextEnabled:bool  console color rich text, default true
GuiEnabled:bool  overlay switch; the setter calls ApplyGui, on in the editor and off in builds
MaxHistory:int  maximum retained overlay lines, default 200
HistoryVersion:int  bumped on every write and clear, used by the GUI for incremental rebuild

Methods:
Verbose(object)  debug log gated by VerboseEnabled
VerboseFormat(string, params object[])  formatted Verbose
Tagged(string tag, object)  Verbose with a module tag
Info / Success / Warning / Error(object)  leveled log inlined into Write; Info, Warning and Error also take a UnityEngine.Object context
InfoFormat / SuccessFormat / WarningFormat / ErrorFormat(string, params object[])  formatted leveled log
Tag(string tag, LogLevel, object)  generic tagged and leveled entry
EnableGui() / DisableGui()  toggle the overlay by hand -> ApplyGui
GetHistory()  read-only list of records
ClearHistory()  clear the records -> HistoryVersion++

Notes:
- This is the only place allowed to call Debug.Log / LogWarning / LogError: those calls are the wrapper's implementation and must not be replaced or routed elsewhere.
- Warning and Error are always emitted regardless of VerboseEnabled; only Verbose and Tagged are gated.
- The overlay is lazily created on the first write (EnsureGuiCreated) and only in Play mode (Application.isPlaying), so EditMode tests get no DontDestroyOnLoad side effects.
