# Contract: ElementContactorObj

Role: contactor element shell; declares its ElementType, its terminals, and a coil-energised latch.

Fields:
points:List<ElementPointObj>  private backing list of this element's terminals
Points:List<ElementPointObj>  read-only accessor used by wiring and managers
Type:ElementType  override returning ElementType.Contactor
coilConnected:bool  runtime latch, default false; true once the coil circuit is energised

Methods:
(none)

Notes:
- Type drives ElementNameMap.GetName(Type) -> symbol "KM"; changing it breaks naming and line matching.
- coilConnected is read by the contactor logic to open and close the NO / NC contacts; resetting it here changes the switching behaviour.
