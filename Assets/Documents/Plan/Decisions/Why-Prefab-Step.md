# ADR: 为什么步骤系统用预制体驱动

> 状态：已采纳 | 日期：2026-07-06

## 决策

步骤系统采用 **GameObject 预制体配置 + 运行时 StepDirector 实例化** 方案，不依赖纯代码或 ScriptableObject 驱动。

## 背景

实验流程（ProjectClip → Task → StepSystem）包含复杂的步骤链。每个步骤需要交互（点击/拖拽/连线/答题等）、动画、音频、提示文字。关键约束：**框架完成后，编辑过程不允许新增或修改代码**——所有内容编辑仅限于 Inspector / Animation / Project / Hierarchy / Scene 面板。

## 备选方案

| 方案 | 编辑成本 | 类型安全 | 零代码编辑 | 结论 |
|------|:--:|:--:|:--:|------|
| **预制体 + 预置 Condition** | 低 | 中（运行时校验） | ✅ | **采纳** |
| ScriptableObject 配置 | 高（需额外编辑器） | 高 | ❌ | 编辑成本过高 |
| 纯代码 | 中 | 高 | ❌ | 每次加步骤需写代码 |
| Timeline 驱动 | 低 | 中 | ✅ | 不适合交互逻辑 |

## 核心设计原则

### 零代码编辑

编辑者通过预置功能完成所有步骤配置。例如：让一个物体显示 → 不需要写 `SetActive(true)`，而是把物体拖到 Condition 的 `activeObjs` 数组里。功能在设计阶段预置，编辑时只做参数化配置。

### P0S0 快进原则

所有步骤跳转都从 P0S0（工序 0、步骤 0）重置并快速执行到目标位置。理由：

> 编辑第 N 步时，不需要考虑前 N-1 步的初始状态。如果我有 100 个步骤要编辑第 99 步，不用把前面 98 步都看一遍。

跳转时用 `99_Loading` 场景遮挡，性能不是瓶颈。

### 三级结构

```
StepSystem → Processing（进程）→ Step（步骤）→ Condition（条件）
```

8 种预置 Condition 类型覆盖全部交互场景：[Default / Click / Drag / Tool / UI / Question / LineConnect / Finish]。每个 Step 绑定一种 Condition，编辑时 Inspector 拖拽配置目标物体和参数。

## 后果

- Controller 在 Step 系统中退化为 StepDirector（轻量调度器），Condition 作为 Step 内的自闭环行为单元
- 步骤系统不完全遵循 MCV，这是经过决策的架构例外
- Animation 配合解决时间轴编辑问题，统一使用 Legacy Animation
