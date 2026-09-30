# Contract: ChnNameMap

Role: central enum to Chinese display-name map; supplies the Chinese names used by the UI and the operation records.

Fields:
ElementChn:Dictionary<ElementType,string>  element kind -> Chinese name
ProbeChn:Dictionary<InspectionProbeType,string>  probe kind -> Chinese name
GearChn:Dictionary<MultimeterGearType,string>  multimeter gear -> Chinese name

Methods:
Get(ElementType)  Chinese name for an element; a missing key falls back to the English enum name
Get(InspectionProbeType)  Chinese name for a probe; a missing key falls back to the English enum name
Get(MultimeterGearType)  Chinese name for a gear; a missing key falls back to the English enum name

Notes:
- Distinct from ElementObjBase.ElementNameMap: that one maps element -> symbol (Resistor -> "R", used to name GameObjects and reverse-look-up types) while this class only supplies display text (Resistor -> "电阻").
- The enums live in Models/EnumAll.cs; every new value must be added here or it silently falls back to the English enum name. ChnNameMapTests guards against misses.
