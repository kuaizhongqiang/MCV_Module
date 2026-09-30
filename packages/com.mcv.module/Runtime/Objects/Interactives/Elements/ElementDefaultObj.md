# Contract: ElementDefaultObj

Role: placeholder / unknown element shell; a valid element that maps to no concrete device type.

Fields:
points:List<ElementPointObj>  private backing list of this element's terminals
Points:List<ElementPointObj>  read-only accessor used by wiring and managers
Type:ElementType  override returning ElementType.None

Methods:
(none)

Notes:
- Type = None makes ElementNameMap.GetName(Type) return "None"; this is the fallback bucket, so it must not be changed to a real device type.
