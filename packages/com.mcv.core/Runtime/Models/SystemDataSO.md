# Contract: SystemDataSO

Role: system data SO; exports StreamingAssets/Data/SystemData.json.

Fields:
data:SystemData  the system data, default new SystemData()

Methods:
Export()  [ContextMenu] override -> ExportData(data)

Notes:
- The data field lives on this concrete SO because Unity cannot serialize a field typed by a generic type parameter.
- The ContextMenu string is Chinese and must stay unchanged.
- The asset instance lives at Assets/Data/ScriptableObjects/SystemDataSO.asset (added 2026-09-30) and its data matches StreamingAssets/Data/SystemData.json; without it the pre-build ExportAll() had no SystemDataSO to export at all.
- Export() writes StreamingAssets/Data/SystemData.json (full overwrite).
- WARNING: the pre-build hook DataSOBuildInit runs DataSOExporter.ExportAll(), which overwrites the JSON from the SO; the menu MCV Editor/危险/创建缺失的数据 SO creates *empty* SOs that would then overwrite SystemData.json with defaults — never run it for this reason.
