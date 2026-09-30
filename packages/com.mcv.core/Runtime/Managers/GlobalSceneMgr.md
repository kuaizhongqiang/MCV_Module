# Contract: GlobalSceneMgr

Role: single entry point for scene load/unload, routing to Addressables or SceneManager, plus the app-quit cleanup chain.

Fields:
CurrentScene:string  last loaded/set scene name (initialised to the active scene in Awake)
IsLoading:bool  guard set around every async load
UseAddressable:bool  injected by Setup; false = plain SceneManager (WebGL fallback)
m_LoadedAAScene:string  the one AA swap scene currently loaded; "" when none

Methods:
Awake()  base + CurrentScene = active scene name
DelayInit()  subscribe SceneSwitchRequestEvent + AppQuitEvent -> isInit
OnDestroy()  unsubscribe both
LoadSceneAdditive(name) / LoadSceneSingle(name)  start the loader when not already loading
LoadScenesAdditive(names)  serial additive load of several scenes
UnloadScene(name)  unload when the name is not CurrentScene
SwitchScene(name)  load the new AA scene additively, then unload the previous; no-op when already loaded
UnloadSwitchedScene()  drop the AA swap scene (back to the resident 1_Content shell)
OnSceneSwitchRequested(e)  -> SwitchScene
OnAppQuitRequested(e)  GlobalAddressableMgr.UnloadAllBundles + ClearAssetCache, then quit (Editor: stop play)
LoadScenesAdditiveAsync / LoadSceneAdditiveAsync / LoadSceneSingleAsync / SwitchSceneAsync / UnloadSwitchedSceneAsync  coroutines
LoadSceneCore(name, mode)  route by IsSceneAA
UnloadAAScene(name)  unload via the AA handle, else via SceneManager
LoadSceneDirectAsync / UnloadSceneAsync  SceneManager wrappers with explicit error/warning logs

Notes:
- Routing is decided by GlobalAddressableMgr.Instance.IsSceneAA; the base scene 1_Content never participates in switching and stays resident.
- Only one AA swap scene exists at a time; SwitchScene skips reloading the current one, otherwise the same scene would be additive-loaded twice.
- IsLoading is reset in finally, so one failing step cannot block every later load or hang the start chain.
- Each load publishes SceneLoadingEvent (progress 1 at the end) and SceneLoadedEvent.
- LoadSceneDirectAsync logs an error when a scene is neither in Build Settings nor an AA entry, instead of failing silently.
- UnloadSwitchedSceneAsync clears m_LoadedAAScene before unloading, so the unload cannot look like "still inside the swap scene".
