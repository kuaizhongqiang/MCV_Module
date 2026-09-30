# Contract: ElementResistorObj

Role: resistor element shell; only declares its ElementType and the terminal list.

Fields:
points:List<ElementPointObj>  private backing list of this element's terminals
Points:List<ElementPointObj>  read-only accessor used by wiring and managers
Type:ElementType  override returning ElementType.Resistor

Methods:
(none)

Notes:
- Type drives ElementNameMap.GetName(Type) -> symbol "R" (shared with Fuse); changing it breaks naming and line matching.
