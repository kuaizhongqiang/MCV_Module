# Managers/Steps —— 步骤导演：进程 → 步骤两级执行

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`../README.md`（管理器约定）；`../../Steps/README.md`（条件与进程组件）

> 路径：`Assets/Scripts/Managers/Steps/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Managers.Steps`

`Steps/`（数据与条件）与运行时交互之间的调度中枢。

## 选文件

- `StepManager.cs` — 步骤导演：协程驱动 Prepare→Waiting→Complete，管延迟/下一步/跳转/跳过

## 跨文件约定

- **场景结构即契约**：`StepManager` 挂在 `1_Content` 内，子节点 `ProcessingHandler` 按 SiblingIndex 命名 `Processing_{index}`（进程），其下 `StepHandler` 生成 id `Step_{p}_{s}`（步骤）。
- **跳转语义 = 快进前缀 + 正常执行目标**（全员归位 → 目标之前 `FastForward` → 目标及之后正常跑）；不要自行实现「只执行目标步骤」，否则动画/显隐状态错乱。
- **条件侧所有 `Waiting` 阻塞点必须用 `ConditionBase.WaitUntilOrForceComplete`**，否则 `NextStep` / 跳转会卡死；`NextStep` / `Skip` 走 `ForceComplete()` 协作式打断。
- **`OnDestroy` 必须停止协程并退订三个入站事件**（`StepNextRequestEvent` / `StepJumpRequestEvent` / `ProcessingJumpRequestEvent`），否则重启场景后会重复推进。
- **`ConditionType.Finish` 特判**：不走三阶段，直接置 `isFinished` 并发 `AllStepsCompletedEvent`；`condition == null` 视为立即完成（连发三事件后延迟跳过）。

## 与其他目录的关系

- 数据与条件：`../../Steps/StepHandler`、`../../Steps/ProcessingHandler`、`../../Steps/Conditions/*`。
- 场景对象：经 `StepHandler` 的显隐/动画方法操作 `Objects/Interactives`。
- 事件：`Event/CoreEvent.cs`、`Event/CoreEventModule.cs`。
- 依赖服务：`GlobalCameraMgr`（条件里的射线）、`GlobalControllerMgr`（条件找 UI 面板）、`ElementManagerBase`（连线条件）。
