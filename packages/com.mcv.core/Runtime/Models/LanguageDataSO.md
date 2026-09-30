# Contract: LanguageDataSO

Role: multi-language data SO; exports StreamingAssets/Data/LanguageData.json.

Fields:
data:LanguageData  the multi-language data, default new LanguageData()

Methods:
Export()  [ContextMenu] override -> ExportData(data)

Notes:
- The data field lives on this concrete SO because Unity cannot serialize a field typed by a generic type parameter.
- The ContextMenu string is Chinese and must stay unchanged.
- LanguageDataSO is a **shared facility with multiple consumers**: besides TextComponent, other scripts (dynamic data) call keys directly through `Lang.Get`. The key table must therefore not assume "every key maps to exactly one text node" (B26).
- A key is just a unique string: path-style keys (containing `/`, produced by auto-registration) and handwritten semantic keys (`ui.xxx`) coexist with no extra rule needed.
- The 65 legacy `ui.*` entries do not take part in auto-registration or reverse lookup — their prefabPath / prefabGuid stay empty.
- The JSON is produced by Export() only; the runtime reads the JSON, never this SO.
