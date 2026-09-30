# Contract: PanelBase

Role: panel base; binds itself to its controller in Start and keeps the registry of components it hosts.

Fields:
m_Canvas:CanvasBase  the canvas that created this panel
m_Components:List<ComponentBase>  registered components
m_LayoutRebuildCoroutine:Coroutine  the in-flight layout rebuild coroutine (a repeat request stops the previous one, so only the last one survives)

Methods:
Start()  virtual; BindController runs once per instance, because every rebuild creates a fresh panel
BindController()  [RequireController] attribute first, else the name convention XxxPanel -> XxxController; a missing target logs an error (attribute) or a warning (convention)
SetCanvas(canvas)  called by CanvasBase.RegisterPanel
RegisterComponent(component) / UnregisterComponent(component)  add / remove, no duplicates
GetUIComponent<T>()  the first registered component of type T, else null
RequestLayoutRebuild(root)  the one way to re-layout after text or child changes: waits a frame, then rebuilds depth-descending via UILayoutRebuilder; root defaults to the whole panel; skipped with a Verbose log while the panel is inactive
OnDestroy()  stops the pending rebuild coroutine, then chains to the base
BreathLightenAnim(images, period, minAlpha = 0.3, maxAlpha = 1)  endless breathing light: one full sine cycle per `period` seconds, mapped from [-1,1] -> [0,1] and then lerped to [minAlpha, maxAlpha]; advances one step per frame and writes only `Image.color.a`, so every image keeps its own RGB tint; null entries are skipped; `period <= 0` falls back to 1.5 s

Notes:
- The breathing light mutates alpha only and advances per frame. The earlier `Color.white * Mathf.Sin(t)` also scaled RGB by a sine that goes negative (illegal alpha, blackened icon) and overwrote each image's own tint, while pairing a per-frame time accumulator with `WaitForSeconds` made the colour jump once per wait instead of breathing.
- Never call LayoutRebuilder.ForceRebuildLayoutImmediate in the same frame as the change: in TMP form TextComponent swaps forms across frames (legacy disabled and unloaded, TMP added one frame later) and a write before `ready` only lands in its pending buffer, so the rebuild measures an empty text; and since the panel prefabs are "child owns a ContentSizeFitter + parent LayoutGroup with childControlWidth = false", the parent measures the child's current sizeDelta, so only child-before-parent rebuilds converge.
- Resolve nodes through the cached TextComponent (TextComponent.NodeOf), never through the Text-typed fields: in TMP form those are fake nulls and every node lookup written against them silently dies (StepUIPanel's old rebuild loop never ran at all).
- One request per frame by design: to refresh several subtrees, pass their common parent instead of calling this twice in a row.
- The attribute path is compile-time safe and is written automatically by the MCV Editor/创建/UI Panel generator; the string convention only exists for older panels.
- Binding happens in Start rather than Awake: a panel is a new instance after every rebuild, so Start marks "bound once per initialisation".
