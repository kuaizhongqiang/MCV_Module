# Contract: SceneAddressableConfig

Role: scene-level AA routing table stored in Resources/; lists which scenes load through Addressables and their addresses, shared by runtime and editor.

Fields:
scenes:List<SceneAAEntry>  [SerializeField] the scenes routed through Addressables
SceneAAEntry  [Serializable] nested type: sceneName (file name without extension), address (runtime Addressables address), sceneAsset (editor-only reference used to resolve the path)

Methods:
GetEntryBySceneName(sceneName)  find the entry by scene name (runtime)
GetEntryByAddress(address)  find the entry by address

Notes:
- GlobalAddressableMgr.IsSceneAA and GetSceneAddress read this table: it is the only decision point for whether a scene uses Addressables.
- The asset lives under Resources/ so the runtime and the editor build share the same data.
