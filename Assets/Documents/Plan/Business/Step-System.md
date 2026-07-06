# 步骤系统

## 概述

实验流程核心执行引擎。不完全遵循 MCV——采用预制体 + 预置 Condition 方案。三级结构：**StepSystem → Processing（进程）→ Step（步骤）→ Condition（条件）**。编辑过程零代码，纯 Inspector 操作。

## 系统层级

```
StepSystem（一个 ProjectClip 一个实例，GlobalStepSystemMgr 管理）
  └── Processing（进程，siblingIndex 决定顺序）
       └── Step（步骤，最小执行单元）
            └── Condition（条件，8 种预置类型）
```

**关键规则**：`siblingIndex` 决定执行顺序，不是名称或 ID。

## 场景层级

```
StepDirector（挂载在 ProjectClip 根节点，随 AB 包加载）
  ├── ProcessingObj_0
  │    ├── StepObj_0（挂 Condition 组件 + 目标引用）
  │    └── StepObj_1
  └── ProcessingObj_1
       └── ...
```

## 三阶段生命周期

```
ExecuteCurrentStep()
  ├── Prepare    → 显隐控制、归位。inactiveObjs 隐藏、activeObjs 显示
  ├── Waiting    → 阻塞等待交互完成。可配置 tipsId（UI 提示）+ audioId（配音）
  └── Complete   → 播放 Legacy Animation → 进入下一步
```

## 跳转规则

所有跳转从 P0S0 快进到目标，期间 `99_Loading` 遮挡。**P0S0 设计理由**：编辑第 N 步时不需要考虑前 N-1 步的初始状态。

FastForward 模式下：Prepare 正常执行 → Waiting 跳过 → Complete 动画瞬间跳到最后一帧（`normalizedTime=1` + `Sample()` + `Stop()`）。

## 8 种 Condition

| 类型 | 交互方式 | 依赖组件 | 适用场景 |
|------|----------|----------|---------|
| **Default** | 无交互，自动播放 | 无 | 过渡动画、自动演示、纯显隐 |
| **Click** | 点击 3D 物体 | StepClickObj（继承 InteractiveBase） | 开关、按钮、仪器点击 |
| **Drag** | 拖拽 A 到目标 B | StepDragObj + StepDragTargetObj | 物体放置匹配 |
| **Tool** | 从面板拖工具到目标 | StepToolPanelController + StepDragTargetObj | 工具选择+拖拽 |
| **UI** | 弹出信息面板，关闭即完成 | StepUiPanelController | 操作说明、图片、视频 |
| **Question** | 单选/多选/判断，答对继续答错重试 | StepQuestionPanelController | 考核环节 |
| **LineConnect** | 3D 端点配对连线 | LineGroup + LineObj + LinePointObj | 电路连接、管线对接 |
| **Finish** | 标记流程完成 | 无 | 每个 Processing 的终结步骤 |

全部 8 种在设计阶段预置，编辑时纯 Inspector 拖拽配置参数和目标引用，**零代码**。

## Step 数据

每个 Step 在 JSON 中包含通用字段：
- **tipsId**：Waiting 阶段 UI 显示提示文字
- **audioId**：Waiting 阶段 GlobalAudioMgr 播放配音

Step Data Model 参考 Tuanjie 项目 `Data/` 路径设计，待细化。

## EventBus 集成

步骤系统通过 EventBus 发布 5 个事件：
- `StepPreparedEvent` / `StepWaitingEvent` / `StepCompletedEvent`
- `ProcessChangedEvent` / `AllStepsCompletedEvent`

UI、音频等模块订阅事件解耦通知。

## 模块关系

| 关联模块 | 关系 |
|---------|------|
| GlobalInteractiveMgr | Condition 通过交互子类接入统一射线检测 |
| GlobalAudioMgr | Waiting 阶段播放配音 |
| GlobalUiMgr | UI/Question/Tool Condition 打开对应 Panel |
| GlobalDataMgr | Step 数据（tipsId/audioId）从 JSON 读取 |
| GlobalStepSystemMgr | 第 9 个 GlobalManager，管理 StepDirector 生命周期 |
| [LineConnect](LineConnect.md) | LineConnect Condition 的底层连线子系统 |

→ [设计决策：Why-Prefab-Step](../Decisions/Why-Prefab-Step.md) | [连线子系统](LineConnect.md)
