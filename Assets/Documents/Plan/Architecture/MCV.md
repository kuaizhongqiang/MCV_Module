# MCV 模式详解

## 概述

本项目采用自研 **MCV（Model-Controller-View）** 架构——漫游与 UI 展示严格遵循，步骤系统经架构决策后适度偏离（见 [Why-Prefab-Step](../Decisions/Why-Prefab-Step.md)）。MCV 是团队针对 Unity MonoBehaviour 下 C（逻辑）与 V（表现）容易耦合这一核心痛点，刻意设计的轻量分层方案。

```
Model      → GlobalDataMgr（数据层，持有 SystemData / ProjectData / UserData）
Controller → ControllerBase<T>（逻辑层，手动放置场景 + 手动注册到 GlobalControllerMgr）
View       → Unity GameObject / UI Panel（表现层）
```

**适用边界**：漫游系统、UI 展示严格遵循 MCV；步骤系统适度偏离，以编辑便利性优先。

## 三层职责

| 层 | 职责 | 不做什么 |
|------|------|---------|
| **Model** | 数据存取、JSON 读写、持有三大数据根对象。纯数据容器 | 不持有 Controller 引用，不订阅事件 |
| **Controller** | 业务逻辑编排、驱动 View 更新、响应 View 事件、注册到 GlobalControllerMgr | 不直接操作 GameObject 属性（由 View 内部处理） |
| **View** | 渲染表现、响应用户输入、通过 IObj 事件反馈交互 | 不包含业务逻辑（校验、状态机、条件判断） |

**核心原则**：Controller 知道一切（Model + View），Model 和 View 互不知晓。数据流单向：View → Controller → Model。

## Controller 生命周期

### 1. 创建

- 手动放置到场景中（不自动生成），以 MonoBehaviour 形式挂载
- 常驻 Controller 放 `1_Controller` 场景，实验级 Controller 放 `N_xxx` 场景
- 泛型约束：`ControllerBase<T>`，T 为对应 View 类型

### 2. 绑定 View

- Inspector 拖拽（推荐）：将 View GameObject/Panel 拖入序列化字段
- 代码赋值：`Awake()` 中 `GetComponent<T>()`，仅当 View 与 Controller 在同一层级时使用
- 绑定时机：必须在注册到 GlobalControllerMgr 之前完成

### 3. 注册

```csharp
protected override void Awake()
{
    base.Awake();
    GlobalControllerMgr.Instance.Register(this);
}
```

在 `Awake()` 中手动注册，早于 `Start()`，确保其他模块可用时已就绪。

### 4. isActive（暂停语义）

- `isActive = false`：停止响应输入和驱动 View，但实例和绑定关系保留
- 典型场景：打开全屏 Panel 时暂停漫游 Controller，关闭后恢复
- 与 `GameObject.activeSelf` 无关——是逻辑层激活状态，不影响渲染

### 5. 销毁

- 场景卸载时随 GameObject 销毁
- `OnDestroy()` 中 GlobalControllerMgr 自动清理引用，无需手动取消注册

## 数据流

```
用户操作 → View（IObj 事件）
              ↓
        Controller（业务逻辑，可调任意 GlobalManager）
              ↓
        Model（数据读写，GlobalDataMgr）
```

Controller 可调用任意 GlobalManager（平级互调），Manager 之间无方向限制。

## EventBus 交互

EventBus 作为独立解耦机制，MCV 体系通过以下方式接入：

- **Step 事件**：StepPreparedEvent / StepWaitingEvent / StepCompletedEvent / ProcessChangedEvent / AllStepsCompletedEvent——UI、音频等模块订阅
- **MCV 内部**：Controller 之间通过 EventBus 协调跨模块状态（如一个 Controller 控制多个 Controller 状态）
- **不替代 MCV 数据流**：EventBus 用于跨模块通知，Module 内部仍走 MCV 单向绑定

## 步骤系统的偏离

步骤系统不完全遵循 MCV，属于经架构决策的例外：

| 偏离点 | MCV 规范 | Step 系统实际 |
|--------|---------|-------------|
| 调度方式 | Controller 驱动 View | StepDirector 直接驱动步骤 |
| 行为单元 | Controller 含业务逻辑 | Condition 为自闭环行为单元 |
| 数据访问 | Controller 通过 GlobalDataMgr | Step 预制体可能直接访问 GlobalDataMgr |

→ [Why-Prefab-Step](../Decisions/Why-Prefab-Step.md) | [步骤系统](../Business/Step-System.md)
