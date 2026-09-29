# Contract: SceneAddressableTools

Role: three-in-one pipeline that registers scenes to Addressables, builds the AA content and removes them from Build Settings.

Fields:
CONFIG_PATH:string  the SceneAddressableConfig asset path
GROUP_NAME:string  the Addressables group name "Scenes"

Methods:
AAPipeline()  register the scenes, build the AA content, then remove them from Build Settings
CleanAAScenes()  remove the Scenes group and its entries
LoadOrCreateConfig() / GetOrCreateSettings() / GetOrCreateGroup(settings)  config and group resolution
RemoveFromBuildSettings(config)  remove the configured scenes from EditorBuildSettings

Notes:
- The pipeline aborts with guidance when the editor is in a script-compile-failed state, because SBP then throws "Error building Player because scripts have compile errors in the editor" and returns within a fraction of a second.
- The build result must be inspected explicitly: ignoring the returned AddressablesPlayerBuildResult would report success even for a failed build.
