# MCV_Module 项目概要

## 定位

通用虚拟仿真实验教学框架，面向理工科（本科/专科/职业教育）。框架不绑定具体课程，任何实验均可通过配置 `ProjectClip` 接入。

**技术栈**：Unity 2022.3.53f1 LTS + URP 14 + C#（.NET Standard 2.1）

团队：2-3 名开发。

---

## 文档索引

### 架构设计

| 文档 | 说明 |
|------|------|
| [MCV 模式详解](Architecture/MCV.md) | Model/Controller/View 职责边界与数据流 |
| [场景加载管线](Architecture/Scene-Pipeline.md) | 0_Setup → 1+2+N 的 Additive 加载机制 |
| [全局管理器](Architecture/GlobalManager.md) | 8 个 GlobalManager 的职责、初始化顺序、依赖关系 |
| [数据层](Architecture/Data-Layer.md) | DataBase 继承链、JSON 持久化、序列化规范 |

### 业务模块

| 文档 | 说明 |
|------|------|
| [漫游系统](Business/Roaming.md) | WASD 移动、鼠标拾取、与 Panel 联动 |
| [UI 展示](Business/UI-Display.md) | 图文/视频组件、TextureReader 模型预览 |
| [步骤系统](Business/Step-System.md) | 预制体规范、步骤调度器、与 MCV 的偏离说明 |

### 功能模块

| 文档 | 说明 |
|------|------|
| [Addressable 资源管理](Modules/Addressable.md) | PackageType 三种加载策略、配置表结构 |
| [交互系统](Modules/Interactive.md) | IObj 接口、事件链、常见交互子类 |
| [UI 框架](Modules/UI-Framework.md) | Canvas/Panel 层级、动画系统、GlobalUiMgr |
| [用户系统](Modules/User-System.md) | 登录注册、进度存档、成绩记录 |

### 数据规范

| 文档 | 说明 |
|------|------|
| [ProjectClip 规范](Data/ProjectClip-Spec.md) | 6 种 TaskData 字段定义与扩展说明 |
| [JSON Schema](Data/JSON-Schema.md) | StreamingAssets/Data 下各 JSON 格式定义 |
| [枚举速查](Data/Enum-Reference.md) | TaskType、PackageType 等枚举一览 |

### 开发指南

| 文档 | 说明 |
|------|------|
| [新增实验](Guides/New-Experiment.md) | 从配置 ProjectClip 到制作场景的全流程 |
| [新增 UI 面板](Guides/New-Panel.md) | 继承 PanelBase 并注册到 GlobalUiMgr |
| [新增交互物体](Guides/New-Interactive.md) | 继承 InteractiveBase 实现 IObj |
| [编码规范](Guides/Coding-Conventions.md) | 命名空间、序列化、协程初始化约定 |

### 决策记录

| 文档 | 说明 |
|------|------|
| [为什么选择 MCV](Decisions/Why-MCV.md) | MCV vs MVC/MVP 的取舍 |
| [为什么步骤系统用预制体](Decisions/Why-Prefab-Step.md) | 预制体 vs 纯代码 vs ScriptableObject |
| [为什么用 Addressable](Decisions/Why-Addressable.md) | Addressables 与 AssetBundle/Resources 的策略 |

---

## 关键术语

| 术语 | 含义 |
|------|------|
| **ProjectClip** | 实验片段，一个完整的教学实验单元，包含 6 个 Task |
| **TaskData** | 任务数据，继承 `TaskDataBase`，每种 TaskType 对应一个子类 |
| **TaskType** | 教学环节枚举：Purpose → Equipment → Principle → LineConnection → Training → Test |
| **GlobalManager** | 全局管理器，继承 `SingletonGlobalMgr<T>`，DontDestroyOnLoad 跨场景持留 |
| **MCV** | 自研架构，Model = GlobalDataMgr，Controller 单向绑定 View |
| **PackageType** | 资源加载策略：Default(Resources)、AA(Addressables)、AB(AssetBundle) |
| **N 场景** | 具体实验的三维场景，按需 Additive 加载卸载 |

---

## 目标终态

可复用的虚拟仿真教学引擎，支撑完整实验流程：

```
登录 → 选择实验 → 任务目的 → 实验仪器 → 实验原理 → 电路连接 → 仿真实验 → 小测验 → 成绩
```

**核心能力**：
- 实验片段（ProjectClip）数据驱动，新增实验无需改代码
- 通用交互系统（点击/拖拽/连线/装配）
- Addressable 按需加载场景与资源
- 用户进度与成绩持久化

---

## 业务形态

三种概念性业务，实际可能交叉：

### 1. 漫游 → [详细文档](Business/Roaming.md)

WASD 移动 + 鼠标点击三维物体，配合 UI Panel 展示信息。典型 3D 场景探索交互。严格遵循 MCV。

### 2. UI 展示 → [详细文档](Business/UI-Display.md)

图文、视频、TextureReader 展示模型。纯 UI 驱动，重内容呈现。严格遵循 MCV。

### 3. 步骤系统 → [详细文档](Business/Step-System.md)

实验流程的核心，步骤链较长。**不完全遵循 MCV**——为便于策划编辑，大量内容在 GameObject 层面配置为预制体，运行时实例化执行。Controller 退化为轻量调度器。

---

## 架构框架

**MCV（Model-Controller-View）** — 漫游与 UI 展示严格遵循，步骤系统适度偏离。→ [详细文档](Architecture/MCV.md)

```
Model      → GlobalDataMgr（数据层，持有 System/Project/User 数据）
Controller → ControllerBase<T>（逻辑层，单向绑定 View）
View       → Unity GameObject / UI Panel（表现层）
```

**运行时场景加载**：→ [详细文档](Architecture/Scene-Pipeline.md)

```
0_Setup → 1_Controller + 2_UI + N（三维实验场景）
```

- `0_Setup`：启动入口，顺序初始化 8 个 GlobalManager
- `1_Controller`：Controller 注册与生命周期管理
- `2_UI`：常驻 UI 层（Menu、Task、Function、Dialog 等面板）
- `N`：具体实验的三维场景，按需加载卸载

**全局管理器**：→ [详细文档](Architecture/GlobalManager.md)

| 管理器 | 职责 |
|--------|------|
| `GlobalAudioMgr` | 音频播放 |
| `GlobalControllerMgr` | Controller 注册查找 |
| `GlobalDataMgr` | 数据持有与 JSON 读写 |
| `GlobalInteractiveMgr` | 交互物体注册与 Raycast |
| `GlobalSceneMgr` | 场景加载卸载 |
| `GlobalUiMgr` | Canvas/Panel 管理 |
| `GlobalAddressableMgr` | 资源统一加载 |
| `GlobalCameraMgr` | 相机管理 |

---

## 当前状态

| 模块 | 状态 |
|------|------|
| 全局管理器（8个） | 框架搭建完成 |
| 数据模型 | System/Project/User 定义完成，JSON 读写就绪 |
| MCV 模式 | IController/ControllerBase 定义完成，业务 Controller 待实现 |
| UI 系统 | Canvas/Panel 基类与 7 个面板骨架完成 |
| 交互系统 | IObj/InteractiveBase 事件接口完成 |
| Addressable | PackageConfigSO/Repository 完成，加载逻辑待完善 |
| 业务内容 | ProjectClip 6 任务数据类定义完成，实际实验内容为空 |

---

## 近期工作（无严格排期，按优先级）

1. **完善数据流** — `GlobalDataMgr` 完整加载 System/Project/User 三份 JSON
2. **场景加载管线** — `GlobalSceneMgr` 实现 Setup → 1+2+N 的 Additive 加载/卸载
3. **漫游基础** — WASD 移动 + 鼠标点击物体检测 → 触发 Panel 展示
4. **UI 展示能力** — 图文组件、视频播放、TextureReader 模型预览
5. **步骤系统** — 设计步骤预制体规范，实现步骤调度器（加载/执行/切换）
6. **交互系统落地** — 连线端点、可拖拽元件、按钮等 `InteractiveBase` 子类
7. **制作示例实验** — 一个简单电路实验跑通全流程（三种业务形态均覆盖）
8. **用户系统** — 登录/注册，成绩记录，进度保存

---

## 命名约定

→ 详见 [编码规范](Guides/Coding-Conventions.md)

- 命名空间跟随文件夹：`MCV_Module.Data.Project`、`MCV_Module.GlobalManager`
- 数据类 `[Serializable]`，`SerializeField` 私有字段 + 公共属性
- 全局管理器通过 `SingletonGlobalMgr<T>.Instance` 访问
- 初始化逻辑写在各管理器的 `DelayInit()` 协程中
