# Contract: ComponentBase

Role: the smallest UI unit; finds its owning panel and registers itself so panels can look components up by type.

Fields:
ComponentType:ComponentType  virtual read-only; the component's kind
panelBase:PanelBase  the panel found with GetComponentInParent

Methods:
Awake()  virtual; base.Awake -> resolve panelBase
Start()  register with the panel when one was found
OnDestroy()  virtual; base.OnDestroy -> unregister

Notes:
- Registration happens in Start and unregistration in OnDestroy, so a panel's registry only ever holds live components.
- The panel is resolved with GetComponentInParent, so a component must sit under its panel in the hierarchy.
