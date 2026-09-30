# Contract: SystemDataSO

Role: system data SO; exports StreamingAssets/Data/SystemData.json.

Fields:
data:SystemData  the system data, default new SystemData()

Methods:
Export()  [ContextMenu] override -> ExportData(data)

Notes:
- The data field lives on this concrete SO because Unity cannot serialize a field typed by a generic type parameter.
- The ContextMenu string is Chinese and must stay unchanged.
