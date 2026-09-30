# Contract: RequireControllerAttribute

Role: compile-time binding attribute from a panel to its controller, replacing the XxxPanel -> XxxController string convention.

Fields:
ControllerType:Type  the bound controller type; it must implement IController and is looked up by type name

Methods:
RequireControllerAttribute(controllerType)  store the type

Notes:
- AttributeTargets.Class with Inherited = false, so a subclass panel must declare its own binding.
- Panels without the attribute fall back to the string convention in PanelBase.BindController, which only exists for older panels.
- The MCV Editor/创建/UI Panel generator writes this attribute automatically.
