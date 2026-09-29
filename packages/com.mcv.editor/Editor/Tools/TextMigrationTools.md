# Contract: TextMigrationTools

Role: half-automatic Legacy `Text` -> `TextComponent` migration; scans prefabs, copies `m_FontData.*` and `m_Color` into component fields, and emits a per-node report.

Menus: `MCV Editor/文字/迁移 dry-run（只报告）` (Run(false)) / `MCV Editor/文字/迁移（落盘，先备份报告）` (Run(true), confirmation dialog)

Fields:
MenuRoot / ReportDirName:const  menu root and report folder
Row  per-node record: asset / nodePath / componentId / fontLabel / fontId / fontSize / fontStyle / alignment / color / text / flag

Methods:
Run(apply)  public for MCP / batch; scans every prefab outside Plugins/ and Packages/
Read(text, assetPath)  reads the Legacy source via SerializedObject into a Row
ResolveFontId(font, out label)  font -> fontId: name contains "digital"/"lcd" -> `digital`, everything else (incl. built-in Arial and SIMHEI) -> `ui`
Apply(contents, text, row)  AddComponent<TextComponent> then writes fontId / text / languageKey(empty) / fontSize / fontStyle(clamped 0-3) / color / alignment / cjkTypography(false); built-in Arial nodes additionally get the asset reference swapped to the FontCatalog legacy path when it is registered
ToOverrideAlignment(textAnchor)  0-8 map 1:1, 9 -> Justified, otherwise Auto
IsInputFieldPart(text) / IsDropdownPart(text)  exception detectors
ReadComponentText(comp) / HierarchyPath(t) / ObjectId(o) / Append(a, b)  helpers
WriteReport(...)  writes `Temp/MCV_TextMigration/report.md` plus `nodes.csv`

Notes:
- Idempotent: nodes that already carry a TextComponent are skipped, and a re-run repairs the early bug that read `m_Text` as `m_FontData.m_Text` and left the component `text` empty.
- Does NOT destroy the Legacy `Text`: removing it would break what-you-see-is-what-you-get. Instance-override transposition is a separate menu (`TextOverrideTools`).
- `m_Text` is a direct field of `Text`, not part of `m_FontData`; only font-related properties live under `m_FontData`.
- Nested prefab instance nodes are skipped on purpose: `GetComponentsInChildren(true)` also returns their `Text`, and adding a component there would push an instance override into the host prefab — the nested asset is handled when it is scanned itself.
- `InputField`'s `m_TextComponent` / `m_Placeholder` are control state, not display copy, so they never take a TextComponent.
- `Dropdown` only supports Legacy `Text`, so Dropdown-bound nodes are flagged "不得换形态" but still migrated.
- Nodes whose alignment is Auto deliberately stay untouched (the seed mechanism in `TextComponent` covers them).
- Finishes with a non-modal `Log.Info` on purpose: a modal dialog would hang the MCP session.
