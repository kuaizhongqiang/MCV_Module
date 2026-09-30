# Contract: ElementCapacitorObj

Role: capacitor element shell; only declares its ElementType and the terminal list.

Fields:
points:List<ElementPointObj>  private backing list of this element's terminals
Points:List<ElementPointObj>  read-only accessor used by wiring and managers
Type:ElementType  override returning ElementType.Capacitor

Methods:
(none)

Notes:
- Type drives ElementNameMap.GetName(Type) -> symbol "C"; changing it breaks naming and line matching.
- Terminals arrive as child ElementPointObj, they are not created here; the list stays empty until points are parented in.
