# Contract: PanelBase

Role: panel base; binds itself to its controller in Start and keeps the registry of components it hosts.

Fields:
m_Canvas:CanvasBase  the canvas that created this panel
m_Components:List<ComponentBase>  registered components
m_LayoutRebuildCoroutine:Coroutine  the in-flight layout rebuild coroutine (a repeat request stops the previous one, so only the last one survives)
m_TextComponents:List<TextComponent>  every TextComponent under this panel, including inactive ones; refreshed by RefreshTextComponents
m_TextWaits:List<TextWait>  in-flight "wait until all text is stable" requests

Methods:
Start()  virtual; BindController runs once per instance, because every rebuild creates a fresh panel; then RefreshTextComponents seeds the text registry
BindController()  [RequireController] attribute first, else the name convention XxxPanel -> XxxController; a missing target logs an error (attribute) or a warning (convention)
SetCanvas(canvas)  called by CanvasBase.RegisterPanel
RegisterComponent(component) / UnregisterComponent(component)  add / remove, no duplicates
GetUIComponent<T>()  the first registered component of type T, else null
RequestLayoutRebuild(root)  **public** (TextComponent's layout step must come through the panel, never straight to UILayoutRebuilder); the one way to re-layout after text or child changes: waits a frame, then rebuilds depth-descending via UILayoutRebuilder; root defaults to the whole panel; skipped with a Verbose log while the panel is inactive
RefreshTextComponents()  rescan GetComponentsInChildren<TextComponent>(true) into m_TextComponents; called in Start and before every WaitAllTextFinished so runtime add/remove (AI bubbles) is counted correctly
WaitAllTextFinished(callback, layer = Layout, timeoutFrames = 0)  protected; call back once **every** TextComponent in the panel reached `layer`; timeoutFrames = 0 means the default (TextComponent.FinishFrameLimit + 2 frames); a timeout **still calls back** (plus a Warning) so a panel can never be wedged by a node that never stabilises; zero components => the callback runs immediately
CompleteOne(wait)  private; one text reached the target layer (an already-satisfied component calls back **in the same frame**, so this can advance while the request is still being registered)
FinishWait(wait) / InvokeWaitSafely(callback)  private; drop the request and invoke the callback with exception isolation
CountdownWait(wait)  private coroutine; one countdown per request, ticking one frame at a time until the wait is done or the budget runs out, then calls FinishWait with a Warning. It is a coroutine rather than an Update hook on purpose: the panel base must not take a per-frame callback, and a coroutine cannot start while the panel is inactive, which is exactly the agreed rule that an inactive panel neither ticks nor calls back (callers must activate the panel or time out themselves)
OnDestroy()  stops the pending rebuild coroutine, then chains to the base
BreathLightenAnim(images, period, minAlpha = 0.3, maxAlpha = 1)  endless breathing light: one full sine cycle per `period` seconds, mapped from [-1,1] -> [0,1] and then lerped to [minAlpha, maxAlpha]; advances one step per frame and writes only `Image.color.a`, so every image keeps its own RGB tint; null entries are skipped; `period <= 0` falls back to 1.5 s

Notes:
- Aggregating `TextComponent.OnFinished` by hand at every business site would duplicate the same bookkeeping, so the panel-level entry is the one to use; the registry is rescanned on every call (not only at Start) because panels add and remove text nodes at runtime.
- The default panel timeout is the component limit + 2 frames: that is engineering headroom on top of the 5–10 frame budget, which is a **component-level** bound and does not apply to the panel.
- One coroutine per wait request: it exits as soon as the wait finishes (all reached or timed out), so the steady-state cost is zero — far cheaper than an `Update` on every panel, and it keeps the base class free of a per-frame callback that subclasses would have to remember to chain.
- `TextWait.done` guards the callback: completion (all texts reached the layer) and timeout are two independent paths, and both go through `FinishWait`, so without the flag a callback could fire twice.
- The breathing light mutates alpha only and advances per frame. The earlier `Color.white * Mathf.Sin(t)` also scaled RGB by a sine that goes negative (illegal alpha, blackened icon) and overwrote each image's own tint, while pairing a per-frame time accumulator with `WaitForSeconds` made the colour jump once per wait instead of breathing.
- Never call LayoutRebuilder.ForceRebuildLayoutImmediate in the same frame as the change: in TMP form TextComponent swaps forms across frames (legacy disabled and unloaded, TMP added one frame later) and a write before `ready` only lands in its pending buffer, so the rebuild measures an empty text; and since the panel prefabs are "child owns a ContentSizeFitter + parent LayoutGroup with childControlWidth = false", the parent measures the child's current sizeDelta, so only child-before-parent rebuilds converge.
- Resolve nodes through the cached TextComponent (TextComponent.NodeOf), never through the Text-typed fields: in TMP form those are fake nulls and every node lookup written against them silently dies (StepUIPanel's old rebuild loop never ran at all).
- One request per frame by design: to refresh several subtrees, pass their common parent instead of calling this twice in a row.
- The attribute path is compile-time safe and is written automatically by the MCV Editor/创建/UI Panel generator; the string convention only exists for older panels.
- Binding happens in Start rather than Awake: a panel is a new instance after every rebuild, so Start marks "bound once per initialisation".
