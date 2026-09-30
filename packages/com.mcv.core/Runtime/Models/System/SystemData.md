# Contract: SystemData (+ ProjectInfo / CopyRight / RenderQuality)

Role: system configuration root; aggregates the software info, copyright, render quality, the UI language and the text form. `LanguageData` / `LanguageClip` are declared in the same file but are **not** fields of `SystemData`.

Fields (SystemData — only these five, plus the inherited DataBase fields):
projectInfo:ProjectInfo  software info (id "ProjectInfo")
copyRight:CopyRight  copyright info (id "CopyRight")
renderQuality:RenderQuality  render quality (id "RenderQuality")
languageType:LanguageType  the UI language, default Chinese; a settings-class field read once at cold start
textType:TextType  the text form (Legacy | TMP), default Legacy; a settings-class field read once at cold start

Same file, independent types (NOT fields of SystemData):
LanguageData.languageClips:List<LanguageClip>  the language entries (LanguageData no longer carries languageType)
LanguageClip.clips:string[]  one slot per LanguageType, opened as empty strings
LanguageClip.prefabPath:string  registration source: the prefab asset path (relative to Assets/, including .prefab); empty for the handwritten keys already in the table
LanguageClip.prefabGuid:string  registration source: the prefab asset GUID — it survives renames and moves, so orphan detection looks at it first

Methods:
ProjectInfo() / CopyRight() / RenderQuality()  fill the default software name, version, company and copyright strings
LanguageClip()  open one empty slot per LanguageType

Notes:
- LanguageClip opens one empty slot per language on purpose so the inspector and JSON show the expected slot count; an all-empty clip makes TextComponent fall back to the static text. Omitting those default slots leaves the inspector showing nothing to fill in.
- The UI language lives in SystemData.languageType, not LanguageData: it is a settings-class field like renderQuality, so writing it back reuses the existing SystemData.json exception instead of punching a new hole in the read-only content data. LanguageData.json now holds only the clip table.
- textType sits right next to languageType because the text form is a settings-class field too (like renderQuality): it lives in SystemData so writing it back reuses the existing SystemData.json exception, and it is read once at cold start — the form is not hot-switched at runtime.
- `LanguageData` and `LanguageClip` are declared in the same file (Models/System/SystemData.cs) but are **not** fields of `SystemData`: SystemData has only the five fields above; the other two are separate serializable types shaping the language table (see Models/LanguageDataSO).
- prefabPath / prefabGuid are the reverse-lookup pair added to LanguageClip for path-style auto-registered keys: prefabPath points at the prefab asset and prefabGuid survives asset renames/moves (so orphan detection checks the GUID first). The handwritten keys already in the table (the 65 `ui.*` entries) leave both empty and are therefore never treated as orphans.
