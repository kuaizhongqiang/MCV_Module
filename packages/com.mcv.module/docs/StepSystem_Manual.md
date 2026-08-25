# 步骤系统手册（StepSystem_Manual）

> 本文档由参考项目 `F:\Project\Tuanjie_Structure\Assets\Scripts\docs\StepSystem_Manual.md` **移植**而来，并针对当前项目 `MCV_Module_0802` 的现有架构做了适配。
>
> **架构差异说明（移植关键）**：
> - 参考项目用 `GlobalStepMgr` + 挂在 ProjectClip 根上的 `StepDirector` + `StepObj`/`ProcessingObj` + 组件式 `StepConditionXxx`。
> - 本项目改用 **`StepManager`（`MCV.Module` 单例导演，场景根节点）+ `ProcessingHandler`/`StepHandler`（MonoBehaviour 节点）+ 纯类 `ConditionBase`（非 MonoBehaviour，按 `conditionType` 创建）**，事件命名与条件契约均已按本项目调整。
> - 凡涉及类型/方法/事件的引用，均为本项目实际类名。

---

## 目录

- [1. 概述](#1-概述)
- [2. 场景结构](#2-场景结构)
- [3. 数据字段](#3-数据字段)
- [4. 步骤执行流程](#4-步骤执行流程)
  - [4.1 核心原则：所有跳转从 P0S0 重置](#41-核心原则所有跳转从-p0s0-重置)
  - [4.2 生命周期 Prepare → Waiting → Complete](#42-生命周期-prepare--waiting--complete)
  - [4.3 状态一致性（动画 + ActiveObj）](#43-状态一致性动画--activeobj)
  - [4.4 快速执行 FastForward / FastComplete](#44-快速执行-fastforward--fastcomplete)
- [5. 条件类型](#5-条件类型)
- [6. 事件驱动](#6-事件驱动)
- [7. 契约面板接口](#7-契约面板接口)
- [8. 跳转与导航](#8-跳转与导航)
- [9. 常见问题与约束](#9-常见问题与约束)

---

## 1. 概述

步骤系统（StepSystem）用于驱动**任务四（电路连接）** 与 **任务五（仿真实验）** 等含多步骤、多条件的教学流程。它采用「**进程 → 步骤 → 条件**」三级结构，由 `StepManager`（步骤导演，`MCV.Module` 包内单例）用**协程**统一驱动，通过 `EventBus` 发布/订阅事件。

**核心特性**：
- 协程驱动，生命周期 `Prepare → Waiting → Complete`。
- 协作式打断：`ForceComplete()` 让 Waiting 协程尽快退出，而非轮询。
- 所有跳转从 P0S0 重置并快速执行到目标（保证动画状态一致）。
- 动画用 Legacy Animation `Play/Sample/Stop` 精确控帧（定格首帧/末帧）。

**适用范围**：步骤逻辑仅在 `LineConnection`（任务四）与 `Training`（任务五）场景中使用（参考 `running-flow.md`）。其余任务（Purpose/Equipment/Principle/Test）不依赖步骤系统。

---

## 2. 场景结构

`StepManager` 挂载在**场景根节点**（与其它管理器同级），其子节点为若干 `ProcessingHandler`（进程），每个进程下再挂若干 `StepHandler`（步骤）：

```
StepManager（MCV_Module.Managers.Steps.StepManager，SingletonBase）
├── ProcessingHandler（进程 0）
│   ├── StepHandler（步骤 0-0）  ── condition（ConditionBase 子类）
│   ├── StepHandler（步骤 0-1）
│   └── ...
├── ProcessingHandler（进程 1）
│   └── ...
```

**节点类**：

| 节点 | 文件 | 职责 |
|------|------|------|
| 步骤导演 | `Managers/Steps/StepManager.cs` | 收集进程、初始化条件、协程执行生命周期、发布状态事件、处理下一步/跳转/跳过 |
| 进程节点 | `Steps/ProcessingHandler.cs` | 承载进程数据，`Awake` 收集子 `StepHandler`（`GetSteps()`），`StepCount` 为步骤数 |
| 步骤节点 | `Steps/StepHandler.cs` | 承载单个步骤数据 + 运行时条件，`Awake` 按 `conditionType` 创建条件 |

**收集规则**（`StepManager.Awake`）：遍历 `transform` 子节点，取 `ProcessingHandler`；`ProcessingHandler.Awake` 遍历其子节点取 `StepHandler`。

---

## 3. 数据字段

### 3.1 步骤节点 `StepHandler`

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | string | 步骤 ID。显式配置优先（避免层级调整失效）；为空时按 `Step_{processIdx}_{stepIdx}` 兜底生成 |
| `displayName` | string | 步骤显示名 |
| `description` | string | 步骤描述 |
| `conditionType` | `ConditionType` | 条件类型（见 §5） |
| `showObjs` / `hideObjs` | GameObject[] | 步骤开始（Prepare）时的显隐物体 |
| `animations` | `List<StepAnimation>` | 动画列表（Legacy Animation + clip + hideOnComplete） |
| `tipsId` | string | 提示 ID |
| `audioId` | string | 音频 ID |
| `targetObj` | `InteractiveBase` | 点击/拖拽目标 |
| `dragObj` | `InteractiveBase` | 拖拽源 |
| `usingId` | string | 工具 / UI / 题目 ID（Tool/UI/Question 用） |
| `lines` | `List<InteractiveBase>` | 连线模板（LineConnect 用；内容实际为 `ElementLineObj`，运行时经 `as ElementLineObj` 转换） |

### 3.2 `StepAnimation`

| 字段 | 说明 |
|------|------|
| `animator` | 持有 Legacy Animation 的物体 |
| `clip` | 动画剪辑 |
| `hideOnComplete` | 播完后是否隐藏该动画物体 |

> ⚠️ `clip` 必须为**非循环动画**，否则 Complete 阶段 `WaitUntil(!AnyAnimationPlaying())` 永不结束。

### 3.3 进程节点 `ProcessingHandler`

- `id` / `displayName`：进程标识。
- 运行时仅提供 `GetSteps()` / `StepCount` 供 `StepManager` 驱动，本身不承载执行逻辑。

---

## 4. 步骤执行流程

### 4.1 核心原则：所有跳转从 P0S0 重置

> **所有跳转都从第 0 进程第 0 步（P0S0）重置并快速执行到目标。** 这是为了保证动画状态一致性。

```
SetStep(2, 3)      ← 任意跳转（无论向前向后）
  → StopAllCoroutines（中断当前执行）
  → PrepareAllConditions()     全员归位：所有条件 Reset + Prepare → 隐藏所有动画物体
  → P0S0 → FF → P0S1 → FF → ... → P2S2 → FF → P2S3（正常执行）★目标
```

**跳转代价与跳转距离成正比**（距离越远，中间 FastForward 的步骤越多）。

**用例子验证**（5 个步骤）：

| 跳转 | 步骤 1 | 步骤 2 | 步骤 3 | 步骤 4 | 步骤 5 |
|------|--------|--------|--------|--------|--------|
| **1 → 3**（向前） | FF→末帧 | FF→末帧 | 正常→首帧等待 | — | — |
| **3 → 2**（向后） | FF→末帧 | 正常→首帧等待 | 后续执行 | — | — |

- **1→3**：1、2 的 Animation 停留在**最后一帧**（"已完成"）；3 从**首帧**正常执行。
- **3→2**：同样从 P0S0 全量重置；1 快进到**末帧**；2 正常执行（**Waiting 才定格首帧**）。

> ⚠️ **关键澄清**：归位阶段动画物体是**隐藏**（`HideAnimations`），**不是定格第一帧**。"定格第一帧"只发生在目标步骤进入 **Waiting** 时（`ShowAnimationsAtFirstFrame`）。这是状态保持一致性的关键。

### 4.2 生命周期 Prepare → Waiting → Complete

每个步骤经历三阶段，由 `StepManager.ExecuteStep` 协程 `yield` 驱动，每阶段完成发布对应事件（§6）：

```
StepManager.ExecuteStep(step, p, s)
  │  lifecycle = StepLifecycle.Prepare
  ├─ yield condition.Prepare()      → 发布 StepPreparedEvent
  │  lifecycle = StepLifecycle.Waiting
  ├─ yield condition.Waiting()      → 发布 StepWaitingEvent
  │  lifecycle = StepLifecycle.Complete
  ├─ yield condition.Complete()     → 发布 StepCompletedEvent
  └─ yield WaitForSeconds(stepDelayTime)  （步骤间延迟 0.5s）
```

**三阶段内做的操作**：

| 阶段 | 通用操作 | 子类钩子 |
|------|----------|----------|
| **Prepare** | `SetObjsActive`（show/hide 显隐）+ `HideAnimations`（动画物体隐藏归位） | `OnPrepare()`（隐藏交互物/关面板/取消临时线） |
| **Waiting** | 子类首行 `ShowAnimationsAtFirstFrame`（动画定格首帧） | `Waiting()`（显示交互目标 + 交互循环，阻塞点用 `WaitUntilOrForceComplete`） |
| **Complete** | `OnCompleteHide` + `PlayAnimations` + `WaitUntil(播完)` + `HideAnimationsOnComplete` | `OnCompleteHide()`（隐藏交互物/关面板） |

**Finish 步骤特判**（`step.Type == ConditionType.Finish`）：不执行三阶段，直接置 `isFinished` + 发 `AllStepsCompletedEvent` 并结束。

**无条件步骤**（`condition == null`）：视为立即完成，连发三个事件后进入步骤间延迟。

### 4.3 状态一致性（动画 + ActiveObj）

**核心**：步骤系统的本质是一个「动画 + 物体显隐」的状态机。视觉状态 = **「Active 显隐」+「被定格在哪一帧」** 的组合。

**状态操作原语**（`StepHandler`）：

| 方法 | 状态操作 |
|------|----------|
| `SetObjsActive()` | showObjs 显示、hideObjs 隐藏（Prepare 设定一次） |
| `HideAnimations()` | 所有动画物体 `SetActive(false)`（归位） |
| `ShowAnimationsAtFirstFrame()` | 动画物体显示 + `normalizedTime=0` + `Play/Sample/Stop`（定格首帧） |
| `PlayAnimations()` | `Play()` 播放动画 |
| `StopAtLastFrame()` | 动画物体显示 + `normalizedTime=1` + `Play/Sample/Stop`（定格末帧） |
| `HideAnimationsOnComplete()` | 按 `hideOnComplete` 隐藏动画物体 |

**精确控帧三连**（状态保持的关键技术）：

```csharp
state.normalizedTime = 0f;      // 或 1f（末帧）
sa.animation.Play();            // 启动播放（进入指定 clip）
sa.animation.Sample();          // 立即采样当前帧 → 物体姿态定格在该帧
sa.animation.Stop();            // 停止播放，物体保持被采样到的姿态
```

- `Sample()` 让动画在**指定帧**把变换应用到物体；`Stop()` 让物体"粘"在那帧不再动。
- `normalizedTime=0` → 定格**首帧**（Waiting 展示开头）；`1` → 定格**末帧**（快进呈现"已完成"）。

**步骤生命周期内的状态切换表**：

| 阶段 | 动画物体 | showObjs/hideObjs | 交互目标 |
|------|----------|-------------------|----------|
| Prepare | 隐藏（归位） | show 显 / hide 隐 | 子类隐藏 |
| Waiting | 显示 + 定格首帧 | 保持 | 子类显示 |
| Complete | 播放中 → 停在末帧 → 按需隐藏 | 保持 | 子类隐藏 |
| FastComplete | 直接定格末帧（不播放）→ 按需隐藏 | 保持 | 子类隐藏 |

> **观察**：`showObjs/hideObjs` 只在 Prepare 设定一次；反复切换的是**动画物体**（隐藏↔首帧↔播放末帧↔定格末帧）与**交互目标**（隐藏↔显示↔隐藏）。

### 4.4 快速执行 FastForward / FastComplete

- **触发**：跳转时，`JumpTo` 对**目标之前**的步骤调用 `FastForward()`。
- **语义**：把"未执行的步骤"的动画**由"未开始（隐藏）"直接拨到"已完成（定格末帧）"**，跳过 Waiting/不播放，瞬间呈现"已完成"姿态。
- **执行链**：`FastForward()`（virtual）→ `FastComplete()`（protected virtual）：`OnCompleteHide()` + `StopAtLastFrame()` + `HideAnimationsOnComplete()`。

**基类 `ConditionBase.FastComplete`**：

```csharp
protected virtual IEnumerator FastComplete()
{
    OnCompleteHide();                       // 子类隐藏交互物/关面板
    step.StopAtLastFrame();                 // 动画瞬间跳到最后一帧
    step.HideAnimationsOnComplete();        // hideOnComplete 处理
    yield break;
}
```

**与正常完成的区别**：

| 对比项 | 正常完成（Complete） | 快进（FastComplete） |
|--------|----------------------|----------------------|
| `OnCompleteHide` | ✅ | ✅ |
| `PlayAnimations()` | ✅ 播放，等播完 | ❌ 不播放，`StopAtLastFrame` 定格末帧 |
| `WaitUntil(播完)` | ✅ | ❌ 不等待 |
| `HideAnimationsOnComplete` | ✅ | ✅ |
| 关键差异 | 有播放过程 | 无播放过程，直接跳末帧 |

---

## 5. 条件类型

`ConditionType`（`Models/EnumAll.cs`）与本项目条件类（均继承 `ConditionBase`，`Steps/Conditions/`）：

| `ConditionType` | 中文 | 条件类 | 条件满足方式 |
|-----------------|------|--------|--------------|
| `Default` | 默认无操作 | `ConditionDefault` | 立即完成 |
| `Click` | 点击交互 | `ConditionClick` | 点击指定 `targetObj` |
| `Drag` | 拖拽交互 | `ConditionDrag` | 将 `dragObj` 拖到 `targetObj` 松开命中 |
| `Tool` | 工具交互 | `ConditionTool` | 从工具面板选工具(`usingId`)拖到 `targetObj` 松开命中 |
| `UI` | UI 交互 | `ConditionUI` | 用 `usingId(uiId)` 弹信息面板，关闭即完成 |
| `Question` | 答题 | `ConditionQuestion` | 用 `usingId(questionId)` 弹题，答对即完成 |
| `LineConnect` | 连线配对 | `ConditionLineConnect` | 所有连线模板端点配对成功 |
| `Finish` | 完成/结束 | `ConditionFinish` | 直接触发全部完成（特判） |

**各条件 Waiting 实现要点**：

| 条件 | Waiting 里显示 | `OnCompleteHide` 隐藏 |
|------|----------------|------------------------|
| `ConditionClick` | `target.SetActive(true)` + 首帧 | 隐藏 `targetObj` |
| `ConditionDrag` | `drag`+`target` 显示 + 首帧 | 隐藏 `dragObj`+`targetObj` |
| `ConditionTool` | 工具面板 + 首帧 | 关面板 |
| `ConditionUI` | 信息面板 + 首帧 | 关面板 |
| `ConditionQuestion` | 答题面板 + 首帧 | 关面板 |
| `ConditionLineConnect` | `ShowLineElements()` + 首帧 | `HideLineElements()` |
| `ConditionDefault` | 仅定格首帧 | 无 |

> ⚠️ **连线特例**：`ConditionLineConnect` **重写了 `FastComplete()`**（L44）：先 `step.Lines` 所有线模板 `SetActive(true)`（呈现"已连完"），再走基类。且连线**不动已提交的常驻连接线**（跳回后仍算完成，状态延续，与"步骤不保留状态"不同）。`CancelDrawing()` 在 `OnPrepare` 里调（应对跳转回来）。

---

## 6. 事件驱动

### 6.1 入口事件（`StepManager.DelayInit` 订阅，强引用 + `OnDestroy` 退订）

| 事件 | 触发动作 |
|------|----------|
| `StepNextRequestEvent` | `NextStep()`：强制完成当前步骤，进入下一步 |
| `StepJumpRequestEvent` | `JumpToStep(stepIndex)`：当前进程内跳转 |
| `ProcessingJumpRequestEvent` | `JumpToStep(p, s)`：跳转进程/步骤 |

### 6.2 状态发布事件（`StepManager` 执行中 `EventBus` 发布）

| 事件 | 时机 |
|------|------|
| `ProcessChangedEvent(p, handler)` | 每次进入新进程 |
| `StepPreparedEvent` | 步骤 Prepare 完成 |
| `StepWaitingEvent` | 步骤进入 Waiting |
| `StepCompletedEvent` | 步骤 Complete 完成 |
| `AllStepsCompletedEvent` | 全部完成 / Finish 步骤触发 |

### 6.3 协作式打断

- **条件状态**：`StepStutus`（`Ready`/`Waiting`/`Complete`），记录在 `condition.Status`。
- **打断机制**：`ForceComplete()` 置 `forceComplete=true` + `Status=Complete`。所有 Waiting 阻塞点必须用 `WaitUntilOrForceComplete(predicate)`（`while(!predicate() && !forceComplete)`），使 `ForceComplete` 能让 Waiting 协程**尽快退出**。
- `StepManager.NextStep` / `SkipCurrentStep` / `CompleteCurrentStep` 都走 `condition.ForceComplete()`。

---

## 7. 契约面板接口

工具 / 信息 / 答题面板通过**接口契约**与条件解耦，条件用 `GlobalControllerMgr.Find("...")` 获取实现（`Interfaces/IStepPanels.cs`）：

| 接口 | 方法/事件 | 获取名 | 对应条件 |
|------|-----------|--------|----------|
| `IStepToolPanel` | `OnToolPressed` / `ShowPanel()` / `SetToolDragging(toolId)` / `ClosePanel()` | `StepToolPanelController` | `ConditionTool` |
| `IStepUiPanel` | `OnPanelClosed` / `ShowData(uiId)` / `ClosePanel()` | `StepUiPanelController` | `ConditionUI` |
| `IStepQuestionPanel` | `OnQuestionCorrect` / `ShowQuestion(questionId)` / `ClosePanel()` | `StepQuestionPanelController` | `ConditionQuestion` |

> 面板实现未找到时，对应条件会告警并跳过（降级），不阻塞流程。

---

## 8. 跳转与导航

`StepManager` 公开方法：

| 方法 | 说明 |
|------|------|
| `StartExecution()` | 从当前索引开始执行（初始 P0S0） |
| `JumpToStep(int p, int s)` | 跳转到指定进程/步骤（全量重置 + 快进前缀） |
| `JumpToStep(int s)` | 当前进程内跳转 |
| `JumpToProcessing(int p)` | 跳转进程（从该进程第 0 步） |
| `SetProcessing(int p)` | 同 `JumpToProcessing` |
| `PrevStep()` | 上一步（同进程上一步 / 上一进程最后一步） |
| `NextStep()` | 强制完成当前步骤，进入下一步 |
| `SkipCurrentStep()` | 同 `NextStep` |
| `CompleteCurrentStep()` | 同 `NextStep` |
| `StopExecution()` | 停止执行，lifecycle 复位 Idle |

**`JumpTo` 内部逻辑**（保证状态一致）：

```
StopAllCoroutines（中断当前）
→ PrepareAllConditions()         全员归位（所有条件 Reset + Prepare，动画隐藏）
→ for p, s：
     目标之前 → condition.FastForward()   （动画定格末帧）
     目标及之后 → ExecuteStep(...)          （正常三阶段）
→ 继续正常推进
```

---

## 9. 常见问题与约束

1. **动画非循环**：`StepAnimation.clip` 必须非循环，否则 Complete 的 `WaitUntil(!AnyAnimationPlaying())` 永不结束。
2. **归位 = 隐藏非首帧**：跳转归位阶段动画物体是**隐藏**，不是定格首帧；"定格首帧"只发生在目标步骤 Waiting。
3. **跳转代价与距离成正比**：从 P0S0 全量重置 + 快进，距离越远中间 FF 步骤越多。
4. **连线状态延续**：`ConditionLineConnect` 跳走再跳回，已提交的常驻连接线不会重置（与"步骤不保留状态"不同）。
5. **协作式打断依赖**：所有 Waiting 阻塞点**必须**用 `WaitUntilOrForceComplete`，否则 `NextStep`/跳转无法打断。
6. **进程/步骤顺序**：`StepManager` 收集**直接子节点**，`ProcessingHandler` 收集**其直接子节点**；层级改动会影响 `id` 兜底生成，建议显式配置 `id`。
7. **Finish 特判**：`ConditionType.Finish` 步骤不执行三阶段，直接触发全部完成。
8. **事件强引用**：`StepManager` 的事件订阅为强引用，`OnDestroy` 必须退订，否则泄漏。

---

> 关联文档：`docs/running-flow.md`（运行流程总览，含步骤系统在任务四/五中的使用）。
