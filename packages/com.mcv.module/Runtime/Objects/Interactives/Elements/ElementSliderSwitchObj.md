# Contract: ElementSliderSwitchObj

Role: slider (linear) switch element shell; only declares its ElementType and the terminal list.

Fields:
points:List<ElementPointObj>  private backing list of this element's terminals
Points:List<ElementPointObj>  read-only accessor used by wiring and managers
Type:ElementType  override returning ElementType.SliderSwitch

Methods:
(none)

Notes:
- Type drives ElementNameMap.GetName(Type) -> symbol "S"; changing it breaks naming and the gear / line matching done by condition steps.
