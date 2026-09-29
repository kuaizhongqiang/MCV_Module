# Contract: TextI18nDataTools

Role: editor-only English-column filler for business data; walks `StreamingAssets/Data/*.json` and adds the `*En` counterpart for every registered Chinese field (missing key -> empty string, existing values untouched).

Menus: `MCV Editor/文字/补齐业务数据英文列 dry-run（只报告）` (Run(false)) / `MCV Editor/文字/补齐业务数据英文列（落盘）` (Run(true))

Fields:
MenuRoot / ReportDirName:const  menu root and the report folder name
Pairs:string[,]  Chinese field -> En column, only for fields actually declared in Models: displayName/description/questionText/itemText/title/contentText/stepTips/opTips/projectName/company/copyright
ListPairs:string[,]  array column pairs: pages -> pagesEn

Methods:
DryRun() / RunApply()  menu entry points
Run(apply)  public for MCP / batch; walks every `*.json` except LanguageData.json, counts added vs already-filled keys, optionally writes back and refreshes the AssetDatabase
Walk(token, report, file, ref added, ref filled)  recursive JToken walk; adds empty En keys for registered pairs, counts existing ones
WriteReport(files, added, filled, lines, apply)  writes `EditorPaths.TempRoot("TextI18nData")/report.md`

Notes:
- Uses JObject/JToken instead of deserialize-then-write-back on purpose: a round trip would drop keys the models do not know (e.g. the legacy `questions[]` in the question bank); key-by-key addition is strictly additive (只加不改).
- LanguageData.json is skipped: its displayName is a development label equal to the key, not user-facing copy, so it must not grow En columns.
- Files are rewritten indented and UTF-8 without BOM; a JSON that fails to parse is skipped with a Warning.
- This is the tool for the business-data English column channel described in `Docs/design_ai/Localization.md` §3 (`Localized.Pick` reads those columns at runtime).
