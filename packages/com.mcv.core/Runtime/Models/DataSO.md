# Contract: DataSO

Role: base ScriptableObject for data authoring; the editor holds the data, the runtime reads JSON, and ExportData writes StreamingAssets/Data/{type name}.json.

Methods:
ExportData<T>(T data)  file name from typeof(T).Name; null data returns immediately -> synchronous write via JsonReaderWriter.Write, editor only
Export()  abstract; each concrete SO implements its own export

Notes:
- Data fields are declared on the concrete SO, not on this base: Unity serialization does not support fields typed by a generic type parameter.
- The synchronous write is editor only (JsonReaderWriter.Write is wrapped in #if UNITY_EDITOR), so Export must not be called at runtime.
- Concrete SOs: SystemDataSO / LanguageDataSO / ProjectDataSO / UserDataSO.
