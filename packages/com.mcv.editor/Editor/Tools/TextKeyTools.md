# Contract: TextKeyTools

Role: the text-key toolchain — key generation and registration, validation, post-save incremental sync, orphan cleanup, prefab rename/move rewriting, plus batch mounting and right-click creation (`Localization.md` §1/§3/§4/§5/§8/§20).

Fields:
DuplicateSuffix:Regex  static readonly; Unity's `" (1)"` de-duplication suffix, stripped so a node's key does not change with the copy count
IllegalChars:Regex  static readonly; whitespace and control characters (kept out of keys; `/` stays as the separator)
KeyRoot / SyncRoot / MountMenu / CreateMenu:string  const; menu paths `MCV Editor/文字/key/…`, `MCV Editor/文字/同步/…`, `MCV Editor/文字/挂载 TextComponent`, `GameObject/UI/TextComponent`
ReportDir:string  static; `Logs/TextSync` (absolute), the report drop point required by §8 (deliberately not `Temp/`)
LogTag:string  const; `[TextKey]`, prefixes every log line

Methods:
IsSyncablePrefab(path)  static; the trigger whitelist — only `.prefab` inside the project, excluding `Assets/StreamingAssets/`, `/Plugins/` and `Packages/`
PrefabPrefix(assetPath)  static; the prefabKey prefix = asset path relative to `Assets/` without `.prefab`
PrefabAssetPathOf(comp)  private; the prefab asset path of a component — via the current PrefabStage when editing in isolation, else via the outermost instance root; null for scene objects
PrefabRootOf(comp)  private; the prefab root the node path starts from (its name is never part of the key)
NodePath(node, root)  static; `/`-joined path from the root's direct children down to the node; empty for the root itself
Segment(t) / SegmentBase(t)  private; one path segment: strip the duplicate suffix, replace whitespace/control characters with `_`, and append `#siblingIndex` only when siblings share the same sanitized name
IsValidKey(key)  static; non-empty, no leading/trailing whitespace, no whitespace or control characters inside
TryBuildKey(comp, assetPath, key, error)  static; full key = `{prefab prefix}/{node path}`; a scene object or a root node is refused with a human-readable reason (not an exception)
TryGetLanguageSO(so, error)  private; the single LanguageDataSO — missing *or duplicated* is a hard precondition error (§4 "multiple SOs" check)
CreateEmptyClips()  private; one empty slot per LanguageType
WriteLanguageKey(comp, key)  private; write via SerializedObject so the prefab stays dirty-tracked
ExportSingle(so)  private; `so.Export()` + SetDirty + SaveAssets — the automatic path must never call `ExportAll()` (§6)
TryRegister(comp, key, error)  static; build the key, refuse a duplicate outright (no overwrite, no auto-suffix), create the clip with the Chinese literal in `clips[0]` and the prefab path + GUID, then write the key and export
RegisterSelected()  menu `登记选中节点的 key`; registers every selected TextComponent and reports failures per node
ValidateAll()  menu `校验全部 key`; reports duplicate ids, illegal keys, half-registered entries (path without GUID or vice versa) and empty Chinese slots; legacy semantic keys are counted but not treated as errors
CleanDryRun() / CleanApply() / Clean(bool)  menu `清理孤儿条目`; §8 order — GUID hit ⇒ refresh the path only; both empty ⇒ not registered, skip; GUID unknown and path gone ⇒ orphan, delete (after writing the report)
AbsolutePath(p)  private; resolve an `Assets`-relative path to an absolute one
WriteRemoveReport(orphans, refreshed, skipped, apply) / Escape(s)  private; the `removed-<timestamp>.json` report written *before* anything is deleted
PrefabMoveHook:AssetPostprocessor  nested; on a moved `.prefab` it rewrites every internal `languageKey` prefix and then refreshes the SO-side `prefabPath` by GUID
RewritePrefabKeys(assetPath, oldPrefix, newPrefix, guid)  private; rewrites only keys carrying the old prefix (legacy semantic keys untouched) and saves the prefab once
RefreshClipPaths(guidToPath)  private; updates `prefabPath` for the entries matching those GUIDs and exports once
AssetSaveHook:AssetPostprocessor  nested; on import of a `.prefab` it queues the path, defers via `EditorApplication.delayCall` and syncs once, then exports once — the three loop guards are the whitelist, the re-entrancy lock and the fact that sync never writes a prefab
SyncPrefab(assetPath)  public; read-only incremental sync — for every TextComponent that already has a key, copy the serialized `text` into `clips[0]` only when it differs (no key generation here, see §3)
CreateTextComponent(command)  menu `GameObject/UI/TextComponent`; creates RectTransform + CanvasRenderer + TMP + TextComponent under the clicked parent
MountSelected()  menu `挂载 TextComponent`; adds TextComponent to every Text/TMP node under the selection, copies fontSize/color/text into it, then registers each one
Mount(go, fontSize, color, literal)  private; the per-node mount + property copy (through SerializedObject, so it is undoable)
IsNestedInsideOtherPrefab(root, node)  private; skips nodes that belong to a nested prefab instance (§17)
SyncSelectedPrefabs()  menu `同步选中 prefab`; manual sync of the selected prefabs through the very same `SyncPrefab` the save hook uses

Notes:
- Key shape: `{prefab asset path relative to Assets/, without .prefab}/{node path without the root name}`; the Chinese editor literal (`text`) is the only input for `clips[0]`, never the runtime `RawText` (which is always empty in the editor — that bug is why newly registered entries used to have an empty Chinese slot).
- Registration is an explicit action (menu / mount / right-click); **the save hook never generates keys**. It only propagates text for nodes that already carry one, and it never writes to the prefab, which is what keeps the hook from re-triggering itself.
- Duplicate keys are blocked, not resolved: the message tells the author to rename the *node* and register again, because an auto-suffix would silently detach the key from the node name.
- Orphan detection follows the GUID first: a moved or renamed asset keeps its GUID, so a "path is gone but GUID resolves" entry is refreshed instead of deleted; the 65 legacy `ui.*` entries have neither field and are always skipped.
- The report for a deletion round is written before the deletion, to `Logs/TextSync/removed-<timestamp>.json`, as required by §8 (never `Temp/`, which is wiped).
- Mounting excludes nested prefab instance nodes and skips nodes that already have a TextComponent (idempotent); registration failures after a mount are logged per node instead of aborting the batch.
- The toolchain is expected to work statically even before the first real panel prefab exists; only the acceptance run of §3/§5/§8 needs one (B29).
