# Steps/Conditions —— 步骤完成条件实现：纯 C# 类，按 Prepare → Waiting → Complete 三阶段被 `StepManager` 协程驱动

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`Steps/StepHandler.cs`（宿主数据）、`Managers/Steps/StepManager.cs`（驱动）、`Interfaces/ICondition.cs` 与 `Interfaces/IStepPanels.cs`（契约）、`Managers/GlobalInteractiveMgr` / `GlobalControllerMgr` / `GlobalCameraMgr`（`ElementManagerBase` 已随业务移除）

> 路径：`Assets/Scripts/Steps/Conditions/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Steps`（与 `Steps/` 根目录同一命名空间）

## 选文件
- `ConditionBase.cs` — 条件基类：三阶段流程 + 协作打断 + 订阅管理
- `ConditionStepPanelBase.cs` — 说明面板条件基类：弹面板 → 等确认 → 关
- `ConditionClick.cs` — 点击 `targetObj` 完成
- `ConditionDrag.cs` — 把 `dragObj` 拖到 `targetObj` 上松开命中
- `ConditionTool.cs` — 从工具面板选 `usingId` 工具拖到目标
- `ConditionUI.cs` — 弹说明面板，点确认即完成
- `ConditionQuestion.cs` — 弹出 `usingId` 题目，答对即完成
- `ConditionFinish.cs` — 终结步骤：确认后整条链结束
- `ConditionDefault.cs` — 默认条件：无交互，立即完成
- `ConditionStart.cs` — 起始标记：确认后进下一步

> 2026-09-30 清理：`ConditionLineConnect`（连线模板）/ `ConditionMeasurePair`（红黑表笔点对）/ `ConditionGearAdjust`（旋钮档位）随元件业务移除；对应 `ConditionType` 枚举值保留作契约（只增不改）。

## 跨文件约定
- **等待必须可被打断**：`Waiting()` 中任何轮询都要写 `while (!IsForceCompleted && !条件)` 或用 `WaitUntilOrForceComplete(...)`；直接用 `WaitUntil` / 长 `WaitForSeconds` 会让 `NextStep` / 跳转卡死或变卡顿。
- **交互订阅成对且幂等**：`SubscribeInteraction(handler)` 进 Waiting 时调用，循环结束与 `ResetCondition()` 都有退订兜底。
- **占位面板要能降级**：`ConditionTool` / `UI` / `Question`（以及共用的 `ConditionStepPanelBase`）在缺失面板控制器时打警告并跳过，保证流程不阻塞 —— 新增这类条件请沿用该模式。
- **射线判定用基类工具**：`RaycastHitTarget` / `IsMouseUp` 已处理「空白处松开不发 Up 事件」的引擎行为，不要自己重写。
- **`ForceComplete` 只置标志**：`Status` 由基类维护，子类不要在 `Waiting` 里自行改 `Status`。
