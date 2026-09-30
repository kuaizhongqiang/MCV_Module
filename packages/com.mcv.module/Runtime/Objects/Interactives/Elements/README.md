# Objects/Interactives/Elements —— 低压电气元件实现层

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：无

> 路径：`Assets/Scripts/Objects/Interactives/Elements/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Objects.Interactives.Elements`

低压电气元件层：全部继承 `ElementObjBase`（→ `InteractiveBase`），按 `ElementType` 区分，挂在 `Managers/ElementManagerBase` 下自注册；端子与导线也在这里。

## 选文件

- `ElementObjBase.cs` — 元件基类：自命名 + 自注册（含 `ElementNameMap`）
- `ElementDefaultObj.cs` — 默认空元件（`None`），可作占位
- `ElementResistorObj.cs` — 电阻（`R`）
- `ElementCapacitorObj.cs` — 电容（`C`）
- `ElementInductorObj.cs` — 电感（`L`）
- `ElementThermistorObj.cs` — 热元件 / 热继电器（`FR`）
- `ElementFuseObj.cs` — 熔断器（符号同为 `R`）
- `ElementContactorObj.cs` — 接触器（`KM`），带线圈已接标记
- `ElementRelayObj.cs` — 继电器（`KA`）
- `ElementTimerRelayObj.cs` — 时间继电器（`KT`）
- `ElementBreakerObj.cs` — 断路器（`QS`），点击分合并抛状态事件
- `ElementButtonSwitchObj.cs` — 按钮开关（`SB`），按/弹沿触发事件
- `ElementKnobSwitchObj.cs` — 旋钮开关（`K`）
- `ElementSliderSwitchObj.cs` — 滑块开关（`S`）
- `ElementPowerObj.cs` — 电源（`P`）
- `ElementMotorObj.cs` — 电动机（`M`），带头部起停动画接口
- `ElementPointObj.cs` — 接线端子：临时线 / 常驻线的端点单元
- `ElementLineObj.cs` — 已连接导线（管状网格），端点对匹配

## 跨文件约定

- 新增元件必须在 `Models/EnumAll.cs` 的 `ElementType` 与 `ElementNameMap.ElementRemap` 同时登记，否则命名退化成 `None`。
- 接线由 `Managers/ElementManagerBase` 状态机驱动：端子只提供 `CreateTmpLine` / `CreateLine` 等被调能力，别自己写拖线循环。
- 判线在 `ConditionLineConnect`：`ElementLineObj.Matches(a, b)` 与 `mgr.GetLines()` 逐条比对（顺序无关），目标连线模板只需预填首尾两端子。
- 元件的显隐/旋转/位移由 `Objects/Tools/ElementAnimation` 提供，元件类只声明 `Type` 与 `Points`。
