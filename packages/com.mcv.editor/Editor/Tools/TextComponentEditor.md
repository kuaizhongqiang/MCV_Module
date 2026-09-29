# Contract: TextComponentEditor

Role: custom inspector for TextComponent that registers or unregisters a language clip in LanguageDataSO and exports JSON.

Methods:
OnInspectorGUI()  show the registration state and the generate and delete buttons
FindLanguageSO()  find the project LanguageDataSO (the first one when several exist)
GenerateClip(so, name)  add a LanguageClip with a new GUID and sync the component plus JSON
DeleteClip(so, name)  remove the matching clip and clear the component
AssignToComponent(id, displayName, clips) / ClearComponentClip()  write or reset the component's languageClip
ReadComponentClips() / CreateEmptyClips()  read the filled clips, or build one empty slot per LanguageType

Notes:
- Marked CustomEditor(typeof(TextComponent)).
- Registering uses a fresh GUID as the id so ids never collide, while displayName stays the object name for readability.
- Text already typed into the component is kept when generating a clip, otherwise empty slots are opened per language.
- The runtime resolves the latest text by clip id from JSON, so every write also re-exports the SO.
- Clearing resets id and displayName to empty, which makes the runtime fall back to the static text.
