# Contract: TitlePanel

Role: title bar panel (View); shows the project name (Chinese and English) with a logo, and slides in/out by animating the layout padding.

Fields:
titleText / engTitleText:Text  project name in Chinese and English
m_TitleTextComp / m_EngTitleTextComp:TextComponent  the two title nodes' components, cached in Awake; in TMP form the component unloads the node's legacy Text (disabled then Destroy; it must be unloaded, because Unity rejects a second Graphic on the same GameObject and AddComponent<TextMeshProUGUI>() returns null), so the Text fields become fake nulls — SetTextOn silently no-ops and GetComponent would throw, which is why SetTitle takes the rect from the cached component
icoImage:Image  logo
m_LayoutGroup:HorizontalLayoutGroup  this object's layout; padding.left is animated
m_LayoutRect:RectTransform  layout rect, force-rebuilt every frame during the animation
offsetLimit:readonly Vector2  (0, -250): shown and hidden padding.left
isActiveNow:bool  initial visible state
m_TargetActive:bool  last requested state, used to debounce duplicate SetUIActive

Methods:
Awake()  cache both title nodes' TextComponents, then validate titleText and icoImage (titleText only counts as missing when its cached component is null too), cache the layout group and rect
Start()  RequestLayoutRebuild, apply the initial state and start DelayStart
DelayStart()  wait for GlobalDataMgr init, then SetTitle(project name, english name)
SetTitle(title, engTitle)  write both texts through the cached components (falls back to TextComponent.SetTextOn), then RequestLayoutRebuild
SetUIActive(isActive)  override; debounce then animate padding and alpha
SetUIActiveImmediately(isActive)  override; stop the animation and apply instantly
ActiveState(isActive)  set alpha, interactable and the target padding, then rebuild
OverrideAnimCoroutine(isActive)  lerp alpha and padding.left, rebuilding every frame

Notes:
- The text-driven rebuilds (Start / SetTitle) go through PanelBase.RequestLayoutRebuild; the two inside ActiveState / OverrideAnimCoroutine stay synchronous on purpose, because the padding tween needs the children re-solved every frame.
- The show/hide animates HorizontalLayoutGroup.padding.left, not spacing; changing spacing breaks the animation.
- After a padding change the layout must be force-rebuilt every frame, otherwise the children jump straight to the end.
- DelayStart waits for GlobalDataMgr.Instance.IsInit; reading the project info before that returns nothing.
- The title texts are not null-checked in SetTitle; the prefab must have both referenced.
