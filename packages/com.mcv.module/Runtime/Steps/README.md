# Steps —— 步骤数据组件与条件状态机：`ProcessingHandler` → `StepHandler` 两级结构，由 `StepManager` 驱动

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`Managers/Steps/StepManager.cs`（驱动方）、`Steps/Conditions/*`（条件实现）、`Objects/Interactives/*`（`targetObj` / `dragObj` / 连线模板）、`Models/StepAnimation.cs`、`Event/CoreEventModule.cs`

> 路径：`Assets/Scripts/Steps/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Steps`

## 选文件
- `ProcessingHandler.cs` — 进程节点：收集子步骤，按序命名
- `StepHandler.cs` — 步骤节点：工程内单个步骤的唯一数据源

## 跨文件约定
- **`ConditionType` → 条件类**：`Click` / `Drag` / `Tool` / `UI` / `Question` / `LineConnect` / `Finish` / `Start` / `MeasurePair` / `GearAdjust` 各对应同名 `Condition*`，其它与 `Default` → `ConditionDefault`（分派在 `StepHandler.CreateCondition`）。
- **步骤 id 稳定性**：Inspector 显式填 `id` 优先；未填时按层级位置生成，改顺序即失效 —— 需跨版本引用的步骤务必显式填写。
- **`animations` 用 Legacy `Animation` 精确控帧**（Play + Sample + Stop）；`showObjs` / `hideObjs` 是步骤级通用显隐，条件类只补自己的特殊处理。
- **条件对象由 `StepHandler.Awake` 创建**，不要在别处 new；跳转依赖 `ResetCondition()` 复位。
- **步骤数据不写 JSON**：新增字段请改 `StepHandler` 的序列化字段，并同步 `Assets/Editor/Tools/StepHandlerEditor`。
