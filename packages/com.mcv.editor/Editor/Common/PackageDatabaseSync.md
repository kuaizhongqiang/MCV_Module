# Contract: PackageDatabaseSync (+ DatabaseAudit)

Role: the only writer of the package master list (PackageDatabaseSO) plus a strictly read-only audit of that list against the expected id set.

Fields:
DatabaseAudit.TotalCount:int  current entry count
DatabaseAudit.Missing:List<string>  expected ids absent from the list
DatabaseAudit.Residue:List<string>  content packages in the list that are not part of this run
DatabaseAudit.NullRefs:List<string>  entries with a lost reference or empty id, matching PackageDatabaseSO.Validate indices

Methods:
Sync(dbAssetPath, justGenerated, logTag)  load-or-create the DB, AutoCollect, backfill the just-generated configs AutoCollect missed, SetDirty and SaveAssets; returns the entry count; the only write path
Audit(dbAssetPath, expectedIds)  read-only diff with no AutoCollect and no disk write; residue is reported for content packages only (AB with a non-empty clipId)
DatabaseAudit.Summary()  one-line summary for dialogs and the console

Notes:
- Assets/Editor/Common is the shared kernel: the write path and the read-only audit must not be duplicated elsewhere.
- Boundary: AutoCollect rewrites the whole packages list, so only Sync (an explicit write call) may invoke it; every dry run must go through Audit.
- Audit never writes and never calls AutoCollect; it only reports.
- Write timing: Sync calls EditorUtility.SetDirty then AssetDatabase.SaveAssets, which is the flush point; Audit touches nothing on disk.
- Null entries in justGenerated are tolerated on purpose (upstream may have deleted broken assets while the list still holds the reference), so do not remove that guard.
- Id comparisons and sorts use StringComparer.Ordinal and logTag prefixes all debug output.
