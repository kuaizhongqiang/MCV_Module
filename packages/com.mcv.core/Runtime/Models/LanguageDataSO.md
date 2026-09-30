# Contract: LanguageDataSO

Role: multi-language data SO; exports StreamingAssets/Data/LanguageData.json.

Fields:
data:LanguageData  the multi-language data, default new LanguageData()

Methods:
Export()  [ContextMenu] override -> ExportData(data)

Notes:
- The data field lives on this concrete SO because Unity cannot serialize a field typed by a generic type parameter.
- The ContextMenu string is Chinese and must stay unchanged.
