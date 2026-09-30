# Contract: LogOverlay

Role: screen-space debug overlay drawn with OnGUI (zero dependencies); created and attached at runtime by the static Log class on the first screen-debug enable and kept on a DontDestroyOnLoad object.

Fields:
m_ContextRect / m_LogRect:Rect  draggable window rects
m_ShowContext / m_ShowLog:bool  panel visibility toggles (F1 / F2)
m_HeaderStyle / m_LabelStyle:GUIStyle  header and body label styles
m_LevelStyles:GUIStyle[]  per-LogLevel colored styles, indexed by (int)LogLevel
m_FontSize:int  label font size, clamped to 8..32 (F4 / F5)
m_Opacity:float  global GUI alpha
m_StylesReady:bool  styles-built flag, lazily built inside OnGUI
m_Fps:float  smoothed FPS
m_SceneState:SceneState / m_TaskType:TaskType  navigation and task values cached from events

Methods:
Awake()  DontDestroyOnLoad plus subscriptions to SceneStateChangeEventData / TaskTypeChangeEventData
OnDestroy()  unsubscribes both events
Update()  smooths FPS and handles the F1..F5 hotkeys
OnGUI()  lazily builds the styles and draws both windows
DrawContextWindow(id) / DrawLogWindow(id)  draggable context panel and log stream (newest on top)
FormatEntry(LogEntry) / StyleForLevel(LogLevel)  format one entry and pick its colored style
BuildStyles() / MakeLevelStyle(color)  style construction, OnGUI only
F3  clears the shared Log history

Notes:
- Created and attached automatically by the static Log class: never place it in a scene or prefab by hand.
- GUI styles can only be created inside OnGUI, so BuildStyles runs lazily through m_StylesReady; moving it to Awake or Build throws.
- The navigation state and task type are cached from the EventBus subscriptions only; they are never polled from managers because the overlay outlives them.
- m_LevelStyles has exactly five slots indexed by (int)LogLevel; StyleForLevel bounds-checks and falls back to m_LabelStyle.
- The log stream is drawn in reverse order (newest first) and older entries overflow and are clipped.
- FPS is smoothed with Mathf.Lerp using unscaledDeltaTime; the window ids are 9001 and 9002 with a 20px drag bar.
