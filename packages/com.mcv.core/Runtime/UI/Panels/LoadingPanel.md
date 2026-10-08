# Contract: LoadingPanel

Role: loading overlay (View); covers the screen while an AA bundle loads or a scene switches.

Fields:
bgImage:Image  background image, receives the sprite built in Init
titleText / contentText:TextComponent  title and content lines
progressText:TextComponent  progress text and the breathing target
progressSlider:Slider  progress bar
breathCurve:AnimationCurve  alpha curve of the breathing progress text
lifeCycle:float  seconds of one full breath (<= 0 falls back to 1)
minShowDuration:float  [SerializeField,Tooltip] minimum time the overlay stays visible (1.2s); read through MinShowDuration
m_ProgressTextComp / m_TitleTextComp / m_ContentTextComp:TextComponent  the three text nodes' components, cached in Awake from the serialized fields; after the `Text` -> `TextComponent` retype the cache is a defensive duplicate (GetComponent returns the same component), and every writer falls back to the field's own SetText / ColorValue
m_BreathCoroutine:Coroutine  handle of the running breathing coroutine

Methods:
Awake()  base first, cache the three text nodes' components, validate the references (background, the three text nodes, the slider — a text node counts as configured when either its field or its cached component is set) and start breathing; logs an error and returns when one is missing
MinShowDuration  property; minShowDuration clamped to >= 0
Init(bgTexture, title, content)  build the background sprite when a texture is given, write title / content through the cached components, reset the progress, start breathing
SetProgress(progress)  write Lang.Get("ui.loading.progress", "{0:0.00}%") through the cached component (falling back to the field) and set the slider value
StartBreath() / StopBreath()  start or stop the breathing coroutine
BreathEffectCoroutine()  loop forever, pulsing the alpha of progressText along breathCurve through ColorValue (falling back to the field's color); exits immediately when the curve or the text node is missing
OnDestroy()  stop breathing, then call base

Notes:
- Show, progress and hide are all driven by LoadingController through SceneLoadingEvent and SceneLoadedEvent; the panel never decides when it appears or disappears.
- The breathing coroutine loops forever, so it must be stopped in OnDestroy; StartBreath is called from both Awake and Init and ignores a second start.
- The cycle coordinate has to be normalised to [0,1] before sampling breathCurve, with lifeCycle = one full breath; otherwise the curve is sampled outside its domain and the pulse does not loop.
- Init(null, ...) is legal (it happens during Awake) and must not dereference the texture.
- The breathing writes alpha only, and the colour must reach the component that currently owns the rendered control (in TMP form the node's legacy Text is unloaded and the field-level `Text` reference would be a fake null) — hence ColorValue rather than a direct colour write.
- Why a text node counts as configured when *either* its field or its cached component is set: after a form swap the legacy control is unloaded, and treating that null as a missing reference would silently kill the breathing effect.
- minShowDuration lives on the panel (a display concern next to breathCurve / lifeCycle); LoadingController only reads it and carries no serialized field of its own.
