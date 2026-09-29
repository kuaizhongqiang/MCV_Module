# Contract: DataSOExporter

Role: initialisation tool exporting every data ScriptableObject to JSON in one full overwrite.

Methods:
ExportAll()  the manual one-click menu that exports every data SO
ExportAllSilent()  the same export without dialogs, for the pre-build hook

Notes:
- Convention: authoring and editing happen in the SO while the runtime reads JSON, so initialisation means overwriting all JSON from the data SOs.
- Triggered either manually from the menu or automatically before a build; both paths run the same export.
