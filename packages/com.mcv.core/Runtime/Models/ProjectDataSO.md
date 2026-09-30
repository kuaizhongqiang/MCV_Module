# Contract: ProjectDataSO

Role: project data SO; exports StreamingAssets/Data/ProjectData.json.

Fields:
data:ProjectData  the project data, default new ProjectData()

Methods:
Export()  [ContextMenu] override -> ExportData(data)

Notes:
- The data field lives on this concrete SO because Unity cannot serialize a field typed by a generic type parameter.
- The ContextMenu string is Chinese and must stay unchanged.
