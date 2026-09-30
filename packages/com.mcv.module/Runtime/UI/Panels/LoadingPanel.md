# Contract: LoadingPanel

Role: loading overlay (View); covers the screen while an AA bundle loads or a scene switches.

Fields:
bgImage:Image  background image, receives the sprite built in Init
titleText / contentText:Text  title and content lines
progressText:Text  progress text and the breathing target
progressSlider:Slider  progress bar
breathCurve:AnimationCurve  alpha curve of the breathing progress text
lifeCycle:float  seconds of one full breath (<= 0 falls back to 1)
m_ProgressTextComp:TextComponent  progress text node's component, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy; it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so the color must go through the component to reach the TMP that is actually rendered
m_TitleTextComp / m_ContentTextComp:TextComponent  the title and content nodes' components, cached in Awake for the same reason; after the swap those Text fields are fake nulls and TextComponent.SetTextOn silently no-ops
m_BreathCoroutine:Coroutine  handle of the running breathing coroutine

Methods:
Awake()  base first, cache the progress / title / content nodes' TextComponents (parsing has to happen before the swap: the swap starts in Awake while Destroy only takes effect at the end of the frame), validate the five references and start breathing; logs an error and returns when one is missing
Init(bgTexture, title, content)  build the background sprite when a texture is given, write title / content through the cached components, reset the progress, start breathing
SetProgress(progress)  write "{0:0.00}% 加载中..." through m_ProgressTextComp (falls back to TextComponent.SetTextOn) and the slider value
StartBreath() / StopBreath()  start or stop the breathing coroutine
BreathEffectCoroutine()  loop forever, pulsing the alpha of progressText along breathCurve; the color goes through m_ProgressTextComp.ColorValue and only the alpha is replaced
OnDestroy()  stop breathing, then call base

Notes:
- Show, progress and hide are all driven by LoadingController through SceneLoadingEvent and SceneLoadedEvent; the panel never decides when it appears or disappears.
- The breathing coroutine loops forever, so it must be stopped in OnDestroy; StartBreath is called from both Awake and Init and ignores a second start.
- The cycle coordinate has to be normalised to [0,1] before sampling breathCurve, with lifeCycle = one full breath; otherwise the curve is sampled outside its domain and the pulse does not loop.
- Init(null, ...) is legal (it happens during Awake) and must not dereference the texture.
- Why the cache is required (visual correctness, not crash safety): the field stays typed `Text` (retyping it would break dozens of prefab references) and the node's legacy Text is unloaded, so in TMP form the field is a fake null and writing `progressText.color` would throw MissingReferenceException; the component's live target is the TMP, so reading and writing ColorValue reaches whatever the component currently owns, in both forms. ColorValue's setter writes the component's own color field, which is acceptable because a panel instance is rebuilt together with its Canvas.
