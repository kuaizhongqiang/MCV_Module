# Contract: LoadingCanvas

Role: persistent loading canvas that hosts only LoadingPanel (the loading mask) and never takes part in state switching.

Fields:
SortingOrder:int  const 1000; above every state canvas and every in-panel canvas
IsPersistent:bool  override true; GlobalUIMgr skips this canvas on state switches
OnRebuild()  override; intentionally empty, the mask is driven by LoadingController through load events

Methods:
Find()  static; the existing canvas from GlobalUIMgr, null when absent, never creates
Ensure()  static; build one at runtime when the scene has none: GameObject with RectTransform, inactive while assembling, DontDestroyOnLoad, Canvas (ScreenSpaceOverlay, sortingOrder 1000), CanvasScaler (1920x1080, MatchWidthOrHeight 0.5), GraphicRaycaster, CanvasGroup, then the LoadingCanvas component last

Notes:
- The mask used to hang under the state canvases (Start / Login / Menu / Content / Roaming), but GlobalUIMgr.SwitchToStateCoroutine fades the current canvas out, calls ClearPanels on every canvas and rebuilds: the mask was torn out and its alpha faded with the parent, so a fast (cache-hit) load made it flicker.
- IsPersistent makes the switch skip this canvas entirely, and sortingOrder 1000 keeps it above everything.
- GraphicRaycaster is required: the event system raycasts per canvas and would otherwise pass clicks through to the state canvas below.
- The script is added last in Ensure so its Awake finds Canvas and CanvasGroup already in place.
- Building it inactive avoids a half-built canvas registering or rendering for a frame.
- DontDestroyOnLoad is deliberate: the mask is global infrastructure and must survive roaming-scene unloads and single-scene switches.
