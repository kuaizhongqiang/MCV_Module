# Contract: ElementPowerObj

Role: power-source element shell; only declares its ElementType and the terminal list.

Fields:
points:List<ElementPointObj>  private backing list of this element's terminals
Points:List<ElementPointObj>  read-only accessor used by wiring and managers
Type:ElementType  override returning ElementType.Power

Methods:
(none)

Notes:
- Type drives ElementNameMap.GetName(Type) -> symbol "P"; changing it breaks naming and line matching.
