# Contract: SystemData (+ ProjectInfo / CopyRight / RenderQuality / LanguageData / LanguageClip)

Role: system configuration root; aggregates the software info, copyright, render quality, the text form and the language entries.

Fields:
projectInfo:ProjectInfo  software info (id "ProjectInfo")
copyRight:CopyRight  copyright info (id "CopyRight")
renderQuality:RenderQuality  render quality (id "RenderQuality")
languageType:LanguageType  the UI language, default Chinese; a settings-class field read once at cold start
textType:TextType  the text form (Legacy | TMP), default Legacy; a settings-class field read once at cold start
LanguageData.languageClips:List<LanguageClip>  the language entries (LanguageData no longer carries languageType)
LanguageClip.clips:string[]  one slot per LanguageType, opened as empty strings

Methods:
ProjectInfo() / CopyRight() / RenderQuality()  fill the default software name, version, company and copyright strings
LanguageClip()  open one empty slot per LanguageType

Notes:
- LanguageClip opens one empty slot per language on purpose so the inspector and JSON show the expected slot count; an all-empty clip makes TextComponent fall back to the static text.
- The UI language lives in SystemData.languageType, not LanguageData: it is a settings-class field like renderQuality, so writing it back reuses the existing SystemData.json exception instead of punching a new hole in the read-only content data. LanguageData.json now holds only the clip table.
- Omitting those default slots leaves the inspector showing nothing to fill in.
- textType sits right next to languageType because the text form is a settings-class field too (like renderQuality): it lives in SystemData so writing it back reuses the existing SystemData.json exception instead of punching a new hole in the read-only content data, and it is read once at cold start — the form is not hot-switched at runtime.
