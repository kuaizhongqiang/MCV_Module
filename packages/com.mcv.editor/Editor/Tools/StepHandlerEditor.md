# Contract: StepHandlerEditor

Role: custom inspector for StepHandler that shows different fields per conditionType.

Fields:
AlwaysFields:string[]  always-shown serialized field names
AlwaysLabels:string[]  Chinese labels aligned with AlwaysFields

Methods:
FieldsByType(type)  the extra field names per condition type
FieldLabel(name)  the Chinese label for an extra field
OnInspectorGUI()  draw the always-shown fields plus the type-specific extras

Notes:
- Marked CustomEditor(typeof(StepHandler)).
- Always shown: id, displayName, description, conditionType, showObjs, hideObjs, animations, tipsId, audioId.
- Type specific: Click shows targetObj, Drag shows targetObj and dragObj, Tool shows usingId and targetObj, UI / Start / Finish / Question show usingId. (LineConnect / MeasurePair / GearAdjust mappings were removed in the 2026-09-30 cleanup.)
- The Chinese label arrays and menu strings are inspector-facing contract and must stay unchanged.
