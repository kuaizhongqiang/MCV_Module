# Contract: Setup

Role: bootstrap MonoBehaviour; waits for every global manager to finish init, then additively loads the base and content scenes and unloads the boot scene.

Fields:
initTimeoutSeconds:float  max wait per manager; on timeout it logs an error and continues
_baseSceneNames:string[]  base scenes loaded additively
_targetSceneName:string  content scene loaded after the base scenes finish
_useAddressable:bool  when true, scene loading goes through GlobalAddressableMgr, otherwise the Build Settings
uiBundleTimeoutSeconds:float  max wait for the UI global package preload; on timeout or failure it logs an error and continues
fontBundleTimeoutSeconds:float  max wait for the font global package preload (B3); on timeout or failure it logs an error and boot continues with the fonts already on the nodes

Methods:
Start()  coroutine: WaitForInit per manager, PreloadFontBundle then PreloadUIBundle (both after GlobalAssetsMgr, before GlobalInputMgr), set GlobalSceneMgr.UseAddressable, then Jump
WaitForInit<T>()  coroutine (T : SingletonGlobalMgr<T>); waits a single manager with its own timeout budget
PreloadGlobalBundle(bundleName, timeoutSeconds, failureHint)  coroutine; waits for GlobalAddressableMgr.IsInit, then preloads one global package; the ready-wait and the preload-wait share ONE timeout budget; any failure only logs and boot continues
PreloadFontBundle()  coroutine; PreloadGlobalBundle for Fonts/font with fontBundleTimeoutSeconds (font is a dependency of the UI bundle, so it runs first)
PreloadUIBundle()  coroutine; PreloadGlobalBundle for UI/ui with uiBundleTimeoutSeconds
Jump() / JumpAsync()  load _baseSceneNames additively, wait for IsLoading, load _targetSceneName, then unload the scene owning this object

Notes:
- There is no host-adapter registration step any more: the highlight and video abstractions were removed on 2026-09-30, so boot goes straight to the manager wait chain.
- Each WaitForInit owns an independent timer, so one manager timing out does not consume another manager's budget.
- On timeout it logs an error and continues on purpose, so a stuck manager cannot deadlock the boot flow.
- GlobalAddressableMgr is awaited only when _useAddressable is true, and a non-null Instance does NOT mean its config map exists (DelayInit sets isInit only after BuildConfigMap); PreloadGlobalBundle therefore waits for IsInit itself, sharing one timeout budget per package. A missing package never blocks boot — it only logs, and the consumer reports the failed fetch later (UIPrefabUtil for panels, TextComponent's fallback for fonts).
- Order matters: the font bundle is a dependency of the UI bundle (panels, fragments and their nested base prefabs reference SIMHEI.TTF), so PreloadFontBundle runs BEFORE PreloadUIBundle — a panel's TextComponent applies its fontId during creation, and a font that arrives later can only fall back to the node's existing font.
- Timeout budgets: the two packages hold SEPARATE budgets (fontBundleTimeoutSeconds / uiBundleTimeoutSeconds) so one cannot starve the other — the font package runs first, and a shared budget would leave the UI package with nothing after the font package times out. Within one package the ready-wait and the preload-wait share a single budget, so the worst case is one timeout, not two.
- The order of WaitForInit calls also matters: GlobalSceneMgr must be ready before UseAddressable is set and before Jump runs.
- Boot finishes by unloading the scene that owns this Setup object; changing that leaves the boot scene loaded.
- The [Header] / [Tooltip] Chinese literals are inspector-facing strings and must stay verbatim.
