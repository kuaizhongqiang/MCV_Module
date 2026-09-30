# Contract: IDataExporter

Role: export interface for data ScriptableObjects; used by the editor batch initialiser (Editor/DataSOExporter).

Methods:
Export()  export this SO's data as JSON

Notes:
- DataSO already implements it, so any new exportable SO should derive from DataSO rather than implement this directly.
