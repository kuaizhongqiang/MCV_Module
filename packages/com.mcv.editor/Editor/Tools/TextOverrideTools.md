# Contract: TextOverrideTools

Role: transposes prefab-instance overrides aimed at Legacy `Text` onto the `TextComponent` (`m_Text` -> `text`, `m_FontData.m_FontSize` -> `fontSize`, `m_FontData.m_FontStyle` -> `fontStyle`).

Menus: `MCV Editor/文字/转置实例覆盖 dry-run（只报告）` (Run(false)) / `MCV Editor/文字/转置实例覆盖（落盘）` (Run(true), confirmation dialog)

Fields:
MenuRoot / ReportDirName:const  menu root and report folder

Methods:
Run(apply)  public for MCP / batch; scans every prefab outside Plugins/ and Packages/, walks the outermost nested-instance roots and rewrites their `PropertyModification` lists
MapProperty(propertyPath, out componentField)  the only mapping table (m_Text / m_FontData.m_FontSize / m_FontData.m_FontStyle); anything else returns null
HasModification(mods, target, propertyPath)  idempotence guard, an already-transposed override is left alone
OutermostInstanceRoots(contents)  every nested-instance root inside the scanned prefab, excluding the prefab itself
WriteReport(...)  writes `Temp/MCV_TextOverride/report.md` with transposed / dangling / skipped sections

Notes:
- Why transposing is mandatory: after migration `TextComponent.Awake` writes its own base values into `Text.text`, so an instance override of `m_Text` gets overwritten and the copy disappears (room HUD names and device names were lost this way).
- Dangling overrides (`mod.target == null`, left over from the TMP era: `m_text` / `m_fontSize` / `m_fontSizeBase`) are never transposed, only reported as "待清理".
- A target that is not a `Text`, or a node without a `TextComponent`, is reported as skipped rather than changed.
- Writes only when something actually changed, through `LoadPrefabContents` / `SaveAsPrefabAsset`; the dry-run pass never touches disk.
- Cancelling the progress bar stops the scan and leaves already-processed prefabs saved as-is (the log states this).
- Finishes with a non-modal `Log.Info` on purpose: a modal dialog would hang the MCP session.
