# Contract: InspectionElementManager

Role: named project-side subclass of ElementManagerBase; carries no logic of its own.

Notes:
- Exists so the scene has a concrete ElementManagerBase instance (ElementManagerBase.Instance) for the resident line system: hand-drawn lines and ConditionLineConnect both depend on it.
- Project-specific wiring that needs a named entry point belongs here; today the base class does everything.
