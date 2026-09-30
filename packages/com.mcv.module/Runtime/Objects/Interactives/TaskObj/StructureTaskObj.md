# Contract: StructureTaskObj

Role: interactive part on the structure page; reports its localized part name. Hover presentation is not part of the framework any more: HoverOrTips only tells the controller whether to show the floating tip.

Fields:
structureName:string  [SerializeField] part name, Chinese column
structureNameEn:string  [SerializeField] English column; empty falls back to Chinese
HoverOrTips:bool  tip switch read by TaskStructureController: true = show the floating tip, false = no hover presentation; public and non-serialized

Methods:
StructureName()  read-only accessor; Localized.Pick(structureName, structureNameEn) by the current language
Awake()  base only
MoClickEvent  empty (click handling is subscribed by InstControlledManager)

Notes:
- This component must sit on the same GameObject as the part's Collider, otherwise no interaction arrives.
- It never touches the UI: the controller owns the tip (MVC), this class only knows who it is.
- Part names are business data that travels with the model prefab (one set per structure model), so they do NOT go into the LanguageData key table or the JSON data: the English column is serialized here and picked at hover time (English empty -> Chinese).
- Prefab edits only reach the runtime after rebuilding the content bundle (`MCV Build/内容 AB 流水线`): the models are loaded from `Content/clip_*`.
