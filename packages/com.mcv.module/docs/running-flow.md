
# 运行流程

> 本文档基于 `Assets/Scripts` 现有实现梳理，标注各环节的**代码落点**（类/文件/方法）与**实现状态**。
> 命名空间 `MCV_Module.*`；路径默认相对 `Assets/Scripts/`。

---

## 一、总体流程

```
进入：Setup → Start → Login → Menu → Task(6 任务自由切换)
返回：Task → Menu → Login
```

- **场景加载方式**：`1_Content` 为基础功能场景（Additive 常驻），`Setup` 为启动场景（加载完成后卸载）。
- **导航状态机**：由 `GlobalUIMgr`（`Managers/GlobalUIMgr.cs`）持有 `SceneState`（Setup/Start/Login/Menu/UI/Roaming）与 `TaskType`（6 类任务），通过 `EventBus<SceneStateChangeEventData>` / `EventBus<TaskTypeChangeEventData>` 驱动 Canvas 重建。
- **实现状态**：Setup / Login / Menu / 部分 Task 已实现；Start / 部分 Task 交互仍为骨架或占位（见各节标注）。

---

## 二、事件驱动导航链路（谁发布 / 谁监听）

> 导航一律走 `EventBus` 驱动，**核心原则：发布方只管发，监听方必须是常驻对象**（`GlobalUIMgr`/`GlobalSceneMgr`/常驻 Controller），避免因 Canvas 随场景销毁重建而丢失监听。

### 1. 场景状态切换 —— `SceneStateChangeEventData`

| 角色 | 对象 | 说明 |
|------|------|------|
| **发布** | `GlobalUIMgr.PublishInitialState`（L143） | 首帧等所有 Canvas 注册后发布初始 `SceneState` |
| **监听** | `GlobalUIMgr.OnSceneStateChanged`（L40） | 重建/切换对应 Canvas |

- ⚠️ 目前仅"初始状态"这一个发布点；进入 Login/Menu/Task 的**状态切换发布点尚未接入**（见各节断点说明）。

### 2. 任务切换 —— `TaskTypeChangeEventData`

| 角色 | 对象 | 说明 |
|------|------|------|
| **发布** | `TaskListPanel.TaskToggleOnValueChanged`（L80） | 用户点击任务 Toggle 时发布 |
| **监听 1** | `GlobalUIMgr.OnTaskTypeChanged`（L41） | 记录当前任务类型 + 重建任务面板 |
| **监听 2** | `TaskListController.OnTaskChanged`（L15） | 同步 `ProjectData.currentTaskType` + 刷新列表显示 |

### 3. 登录成功 —— `LoginSuccessEvent`

| 角色 | 对象 | 说明 |
|------|------|------|
| **发布** | `LoginController.OnLoginRequested`（L43） | 白名单通过后发布 |
| **监听** | **（无）** ⚠️ | 注释声明"后续在此订阅做场景切换"，当前**无人监听**——登录→Menu 导航断点 |

### 4. 场景切换请求 —— `SceneSwitchRequestEvent`

| 角色 | 对象 | 说明 |
|------|------|------|
| **发布** | 待接入（Roaming 加载时） | 事件驱动加载 AA 场景 |
| **监听** | `GlobalSceneMgr.OnSceneSwitchRequested`（L35） | 已订阅，`SwitchScene` 先加载新 AA → 卸载旧 AA |

> **断点汇总**：`LoginSuccessEvent` 无监听方（登录→Menu 断）、Menu 叶子点击无发布事件（Menu→Task 断）、`SceneSwitchRequestEvent` 发布方未接入。这些是后续导航补全的切入位置。

---

## 三、进入流程

### 1. Setup（初始化，独立场景）

- **代码落点**：`Setup.cs`
- **职责**：初始化 Unity、逐个等待全局管理器就绪、注册宿主插件适配器、加载 Content 场景后卸载自身。
- **管理器初始化顺序**（每个独立 15s 超时，超时告警后继续，避免启动卡死）：
  1. `GlobalAudioMgr` → `GlobalControllerMgr` → `GlobalDataMgr` → `GlobalInteractiveMgr` → `GlobalSceneMgr` → `GlobalUIMgr`
  2. `GlobalAddressableMgr`（仅 `UseAddressable=true` 时）
  3. `GlobalCameraMgr` → `GlobalAssetsMgr` → `GlobalInputMgr` → `GlobalAiMgr`
- **适配器注册**（`RegisterHostAdapters()`，幂等）：`IHighlightService.Register(new HighlightPlusAdapter())` + `AVProVideoPlayerAdapter.Register()`。
- **场景跳转**（`JumpAsync`）：
  1. `LoadScenesAdditive(_baseSceneNames)`，默认 `["1_Content"]`（Additive 加载功能场景）。
  2. 等待 `IsLoading` 结束。
  3. `_targetSceneName` 非空时 `LoadSceneAdditive(_targetSceneName)`（内容场景）。
  4. `UnloadScene(gameObject.scene.name)` 卸载启动场景。

### 2. Start（欢迎/开始界面）

- **代码落点**：`Controllers/StartController.cs`、`UI/Panels/StartPanel.cs`
- **设计**：利用 Content 场景的 `StartCanvas`，播放视频（可跳过），停留界面，点击按钮进入 Login。
- ⚠️ **不可逆**：进入后无法返回 Start。
- **实现状态**：`StartController.OnViewBound()` 为空、`StartPanel` 为空白壳 —— **视频播放、跳转按钮均未实现（骨架）**。

### 3. Login（登录）

- **代码落点**：`Controllers/LoginController.cs`、`UI/Panels/LoginPanel.cs`、`Managers/GlobalDataMgr.cs`、`Models/User/UserData.cs`
- **三种身份**：游客 / 学生 / 教师（`UserType`，下拉排除 Admin）。游客只需用户名，学生/教师需用户名+密码（`LoginPanel.L126-142` 校验）。
- **登录流程**（`LoginController.OnLoginRequested`）：
  1. 取 `UserName/Password/UserType` → `GlobalDataMgr.VerifyLogin()` 验证。
  2. 失败 → `View.ShowTipsError`；通过 → `GlobalDataMgr.SetUserData()` 写入 `UserData`（userName/password/userType/loginTime）。
  3. 发布 `EventBus<LoginSuccessEvent>.Publish(new LoginSuccessEvent(user))` → `ShowTipsSuccess`。
- **登录状态保持**：由 `GlobalDataMgr` 持有 `UserData`，可登出后重新登录。
- ⚠️ **实现状态**：`VerifyLogin()` 目前为占位（直接 return true）；**登出/退出登录逻辑尚未实现**（代码中无 Logout）。

### 4. Menu（菜单）

- **代码落点**：`Controllers/MenuController.cs`、`UI/Panels/MenuPanel.cs`、`Models/Project/ProjectData.cs`
- **职责**：一级目录 → 二级目录，选择叶子菜单后进入对应项目的第一个 `TaskData`。
- **业务状态**（跨 Canvas 重建存活）：`currentClips`（当前层级兄弟）、`currentParent`（父菜单，null=根）、`selectedClip`（当前选中）。
- **层级逻辑**：
  - `EnterRoot()`：进入根层级（`GetRootMenus()`，parentId 为 null 的菜单）。
  - `EnterChildren(parent)`：下钻子层级（`GetChildMenus(parent)`）。
  - `GoBack()`：返回上一层级，焦点定位在刚下钻的父菜单。
  - `OnMenuSelected(clip)`：有子菜单则下钻，否则记为**最终选中（叶子）**——当前仅 `Debug.Log`，**预留后续动作**（进入 Task 未接入）。
- **数据**：`MenuClip`（id/displayName/parentId + 绑定的 `ProjectClip`）；`MenuData.GetRootClips/GetChildClips/HasChildren` 提供层级查询。
- ⚠️ **注意点**：`MenuController` 在无数据时会注入测试菜单数据（`BuildTestData()`）。

#### Menu → Task 的「点击持有」问题（当前断点）

> **结论：Menu 点击叶子菜单目前没有任何事件发出、也没有任何监听方，进入 Task 的链路是断的。**

- **为什么需要 EventBus 驱动**：Menu Canvas 在切换场景时会被销毁重建，`MenuController` 与 `MenuPanel` 不保证常驻。若在 `MenuController.OnMenuSelected` 里直接引用 Task 相关对象，会因 Canvas 重建失效。正确做法是：**点击叶子菜单 → 发布事件 → 由常驻的监听方接管**。
- **当前状态**：
  - `MenuController.OnMenuSelected`（叶子）仅 `Debug.Log`，**未发布任何进入 Task 的事件**。
  - `LoginSuccessEvent`（`CoreEvent.cs` L96）注释已声明"登录成功后的执行暂为空，后续在此订阅做场景切换"——**当前无订阅方**。
- **建议的监听方**（事件驱动应选**常驻**对象，避免随 Canvas 销毁）：
  - `LoginSuccessEvent`（登录成功 → 进入 Menu）：由 `GlobalUIMgr`（常驻单例，已订阅 `SceneStateChangeEventData`/`TaskTypeChangeEventData`）或新增常驻 `NavigationController` 订阅，发布 `SceneStateChangeEventData(Menu)` 切换状态。
  - Menu 点击叶子（进入 Task）：复用同样的导航事件（`SceneStateChangeEventData(UI/Roaming)` + `TaskTypeChangeEventData`），由 `GlobalUIMgr` 统一监听重建 Canvas、`GlobalSceneMgr` 监听切换 AA 场景。
- **⚠️ 待办**：叶子菜单进入 Task 的具体事件与监听方尚未实现，需在 `MenuController.OnMenuSelected` 里发布，并补订阅。

### 5. Task（任务，6 种）

- **特性**：
  - 任一任务可随时切换、随时返回。
  - **不保留状态**，每次进入从头开始。
  - 任务可能是 UI（`SceneState.UI`）也可能是 Roaming（`SceneState.Roaming`）；Roaming 进入需走 Loading + Addressables 包。

#### Task 切换：`TaskListPanel` 发布 → 谁监听

> **结论：任务切换由 `TaskListPanel.cs` 发布 `TaskTypeChangeEventData`，由 `GlobalUIMgr` 和 `TaskListController` 两个监听方接管。**

**事件**：`TaskTypeChangeEventData`（`Event/CoreEvent.cs` L124，含 `Clip` + `TaskType`）

**发布方（唯一点）**：`UI/Panels/TaskListPanel.cs`
- `Init(project, taskType)`（L50）：装配项目任务 Toggle，勾选当前任务项，挂切换监听。
- `TaskToggleOnValueChanged(type)`（L77）：用户点击任务 Toggle → `EventBus<TaskTypeChangeEventData>.Publish(...)`（L80）。

**订阅方（两个，职责分离）**：

| 监听方 | 订阅点 | 职责 |
|--------|--------|------|
| `Managers/GlobalUIMgr.cs` | `DelayInit` 订阅 `OnTaskTypeChanged`（L41） | 记录 `m_CurrentTaskType` + `m_ActiveCanvas.Init()` 重建任务面板 |
| `Controllers/TaskListController.cs` | `Awake` 订阅 `OnTaskChanged`（L15） | 同步 `GlobalDataMgr.ProjectData.currentTaskType` + 刷新列表显示 `SetTaskType` |

- **订阅时机**：`GlobalUIMgr` 在 `DelayInit` 常驻订阅（强引用，`OnDestroy` 退订）；`TaskListController` 在 `Awake` 订阅（常驻 Controller）。两者都是**常驻对象**，不随 Canvas 销毁，故能稳定接收任务切换事件。
- **`TaskListPanel` 只发布不监听**：它负责把用户点击翻译成事件，不自己处理切换逻辑（显示与逻辑分离，`SetToggleState` 只改显示不触发 `onValueChanged`）。

- **数据模型**（`Models/Project/ProjectData.cs`）：
  - `ProjectData`：含 `List<ProjectClip>` + 运行时 `currentClip` / `currentTaskType`。
  - `ProjectClip`：绑定 6 个 `TaskData` 子类（Purpose/Equipment/Principle/LineConnection/Training/Test），`Tasks` 属性按序聚合，`GetTaskData<TaskType>()` 按类型取。
  - `TaskData<T>`：抽象基类，含 `TaskActive`（是否启用）字段 —— **Menu 进入 Task 时应过滤未激活任务**（文档原 TODO）。
- **场景切换**（`Managers/GlobalSceneMgr.cs`）：
  - `SwitchScene(sceneName)`：先 Additive 加载新 AA 场景，完成后卸载上一个 AA 场景（`m_LoadedAAScene`），任意时刻只有一个 AA 切换场景，`1_Content` 常驻。
  - 加载按 `GlobalAddressableMgr.IsSceneAA()` 路由：AA 走 Addressables，否则走 SceneManager。

---

## 四、任务模块明细

### 任务一 · 任务目的（`TaskType.Purpose`）

- **代码落点**：`UI/Panels/TaskPurposePanel.cs`、`Models/Project/TaskPurposeData`
- **数据**：`contentText`（文字）、`prefabKey`（模型/图片）。
- **功能**：展示文字或图片、播放音频；展示实例化模型动画，通过 TextureRender 映射到 UI。
- **实现状态**：面板有基础实现（`GetPanelContent` 返回任务目的描述）。

### 任务二 · 实验仪器（`TaskType.Equipment`）

- **代码落点**：`UI/Panels/TaskEquipmentPanel.cs`、`Models/Project/TaskEquipmentData`、`InputController/FocusRotationController/FocusRotationControl`
- **数据**：`List<EquipmentStruct>`（prefabKey/title/contentText/audioName）。
- **功能**：展示列表，初始化选第一个，可点击切换；实例化模型映射到 UI；`FocusRotationControl` 控制相机。
- ⚠️ **技术点**：URP 相机开启 post-processing 时 alpha 通道恒为 1，`RawImage` 需加 Shader；需另开一台相机单独取 alpha 通道。

### 任务三 · 实验原理（`TaskType.Principle`）

- **代码落点**：`UI/Panels/TaskPrinciplePanel.cs`、`Models/Project/TaskPrincipleData`
- **数据**：`List<PrincipleStruct>`（title/contentText/videoName）。
- **功能**：视频播放器带列表，可切换原理视频。

### 任务四 · 电路连接（`TaskType.LineConnection`）

- **代码落点**：`UI/Panels/TaskLineConnectionPanel.cs`、`Steps/Conditions/ConditionLineConnect.cs`、`Objects/Tools/LineDraw.cs`、`Managers/ElementManagerBase.cs`
- **核心**：步骤逻辑 + 3D 连线交互。
- **连线周期**（一个步骤的完整交互）：
  ```
  开始 → 第一个点 → 生成临时线 → 第二个点 → 判断
    ├─ 配对成功 → 移除临时线，生成最终线
    └─ 配对失败 → 回到开始
  ```
- **临时线**：两个点，起始点为选中的点，第二个点为距离相机一定距离的某个平面中、鼠标映射屏幕空间的点。
- **最终线**：预设好的线，可能由 `LineDraw`（管状网格，Catmull-Rom 样条 + sin² 位移）生成，也可能是 fbx 导入的线。
- **连线判定**（`ConditionLineConnect`）：`step.Lines` 存目标连线模板（`ElementLineObj`，inactive，PointList 预填两端点）；轮询 `ElementManagerBase` 已连接的线，所有模板端点对都有匹配即完成（顺序无关）。跳转离开时 `CancelDrawing` 取消临时线，不销毁已提交的常驻线（跳回后仍算完成，状态延续）。
- **2D 连线**：不使用 LineDraw，含临时线与最终线，但**现阶段不处理**。
- **实现状态**：步骤/连线框架已实现；面板层 `TaskLineConnectionPanel` 为占位。

### 任务五 · 仿真实验（`TaskType.Training`）

- **代码落点**：`UI/Panels/TaskTrainingPanel.cs`、`Models/Project/TaskTrainingData`
- **核心**：步骤逻辑（见"步骤逻辑"一节）。
- **实现状态**：面板为占位（`GetPanelContent` 返回空），待接入仿真实验流程。

### 任务六 · 小测验（`TaskType.Test`）

- **代码落点**：`UI/Panels/TaskTestPanel.cs`、`Models/Project/TaskTestData`、`QuestionData`/`QuestionClip`（`Models/Project/ProjectData.cs` 关联）
- **设计**：一个预制的选择题小游戏，多题逐题作答，答完进入下一题，直到结束。
- **实现状态**：面板为占位（`GetPanelContent` 能描述当前题/选项/正确项，但完整的答题交互流程未实现）。

---

## 五、步骤逻辑

步骤系统采用「**进程 → 步骤 → 条件**」三级结构，由 `StepManager`（步骤导演）用**协程驱动**统一执行。本文档基于现有代码实际描述，无需参考旧项目。

### 1. 层级结构与代码落点

```
StepManager（步骤导演，SingletonBase）
├── ProcessingHandler（进程 0）
│   ├── StepHandler（步骤 0-0）── condition（ConditionBase 子类）
│   ├── StepHandler（步骤 0-1）
│   └── ...
├── ProcessingHandler（进程 1）
│   └── ...
```

| 节点 | 文件 | 职责 |
|------|------|------|
| 步骤导演 | `Managers/Steps/StepManager.cs` | 统一驱动所有进程/步骤，协程执行生命周期，发布状态事件，处理下一步/跳转/跳过 |
| 进程节点 | `Steps/ProcessingHandler.cs` | 承载进程数据，`Awake` 收集子 `StepHandler`（`GetSteps()`） |
| 步骤节点 | `Steps/StepHandler.cs` | 承载单个步骤数据 + 运行时条件，`Awake` 按 `conditionType` 创建条件 |
| 条件基类 | `Steps/Conditions/ConditionBase.cs` | 三阶段生命周期 + 协作式打断 + 订阅管理 |

### 2. 步骤数据（`StepHandler` 序列化字段）

| 字段 | 用途 |
|------|------|
| `id` / `displayName` / `description` | 步骤标识。`id` 显式优先（Inspector 可配，避免层级调整失效），为空时按层级 `Step_{processIdx}_{stepIdx}` 兜底生成 |
| `conditionType` | 条件类型（`ConditionType` 枚举，见下） |
| `showObjs` / `hideObjs` | 步骤开始（Prepare）时的显隐物体 |
| `animations` | `List<StepAnimation>`（Legacy Animation + clip + hideOnComplete） |
| `tipsId` / `audioId` | 提示 / 音频 ID |
| `targetObj` / `dragObj` | 点击目标 / 拖拽源（`InteractiveBase`） |
| `usingId` | 工具 / UI / 题目 ID（`ConditionTool/UI/Question` 用） |
| `lines` | 连线模板（`ConditionLineConnect` 用） |

### 3. 条件类型（`Models/EnumAll.cs` `ConditionType`）

| 类型 | 中文 | 条件满足方式 |
|------|------|--------------|
| `Default` | 默认无操作 | 立即完成 |
| `Click` | 点击交互 | 点击指定 `targetObj` |
| `Drag` | 拖拽交互 | 将 `dragObj` 拖到 `targetObj` 松开命中 |
| `Tool` | 工具交互 | 从工具面板选工具(`usingId`)拖到 `targetObj` 松开命中 |
| `UI` | UI 交互 | 用 `usingId(uiId)` 弹信息面板，关闭即完成 |
| `Question` | 答题 | 用 `usingId(questionId)` 弹题，答对即完成 |
| `LineConnect` | 连线配对 | 所有连线模板端点配对成功 |
| `Finish` | 完成/结束 | 直接触发全部完成（特判） |

**条件类**：`ConditionDefault` / `ConditionClick` / `ConditionDrag` / `ConditionTool` / `ConditionUI` / `ConditionQuestion` / `ConditionLineConnect` / `ConditionFinish`，均继承 `ConditionBase`（纯类，非 MonoBehaviour）。

### 4. 三阶段生命周期（`ConditionBase`）

每个步骤经历 **Prepare → Waiting → Complete** 三阶段，由 `StepManager` 协程 `yield` 驱动：

```
Prepare   ：通用显隐(showObjs/hideObjs) + 隐藏动画物体归位 + 子类 OnPrepare 钩子
            （子类隐藏交互物/关面板/取消临时线）
Waiting   ：子类实现交互循环（阻塞点必须用 WaitUntilOrForceComplete）
            （订阅全局交互事件 → 等待条件满足 → 退订）
Complete  ：子类 OnCompleteHide 隐藏交互物 → PlayAnimations 播放 → 等播完
            → HideAnimationsOnComplete(hideOnComplete 处理)
```

**对应钩子**：`OnPrepare()` / `abstract Waiting()` / `OnCompleteHide()`。

### 4.1 动画与 ActiveObj 的状态保持 —— 核心机制

> **步骤系统的本质是一个"动画 + 物体显隐"的状态机**。每个步骤执行的是对**一批物体（`showObjs`/`hideObjs`/`animations` 物体/交互目标）**的 Active 状态和 **Legacy Animation** 播放状态的操作。核心问题永远是：**每个阶段，这些物体和动画应该处于什么状态、如何被设定、如何保持**。快进/跳转只是状态转换的一种场景。

#### (1) 状态操作原语（`StepHandler`，`Steps/StepHandler.cs`）

| 方法 | 对状态的操作 | 说明 |
|------|--------------|------|
| `SetObjsActive()` | showObjs `SetActive(true)`；hideObjs `SetActive(false)` | **通用显隐应用**：本步骤"该显示谁、该隐藏谁" |
| `HideAnimations()` | 所有动画物体 `SetActive(false)` | **动画归位**：把动画物体整体隐藏（未开始时） |
| `ShowAnimationsAtFirstFrame()` | 动画物体 `SetActive(true)` + 设 `clip` + `normalizedTime=0` + `Play/Sample/Stop` | **定格首帧**：显示动画物体并采样到第 0 帧后停止 |
| `PlayAnimations()` | 设 `clip` + `Play()` | **播放**：让动画真正动起来 |
| `StopAtLastFrame()` | 动画物体 `SetActive(true)` + `normalizedTime=1` + `Play/Sample/Stop` | **定格末帧**：显示并采样到第 1 帧后停止 |
| `HideAnimationsOnComplete()` | `hideOnComplete` 的动画物体 `SetActive(false)` | **完成态隐藏**：播完后按需隐藏 |

#### (2) 核心：`Play(); Sample(); Stop();` 三连 = 精确控帧/定格

```csharp
state.normalizedTime = 0f;      // 或 1f（末帧）
sa.animation.Play();            // 启动播放（进入指定 clip）
sa.animation.Sample();          // 立即采样当前帧 → 物体姿态定格在该帧
sa.animation.Stop();            // 停止播放，物体保持被采样到的姿态
```

这是**状态保持的关键技术**：
- **`Sample()`** 让动画在**指定帧**把变换应用到物体上；
- **`Stop()`** 立即停止，**物体就"粘"在那一帧的姿态上不再动**。
- `normalizedTime=0` → 定格**首帧**（Waiting 展示动画开头姿势）；`normalizedTime=1` → 定格**末帧**（快进呈现"已完成"姿势）。
- 视觉状态 = **「Active 显隐」+「被定格在哪一帧」** 的组合，两者都是显示层状态，靠 `SetActive` 和 `Play/Sample/Stop` 共同维持。

#### (3) 一个步骤的生命周期 = 四套状态切换

步骤执行过程中，动画/物体状态按阶段被反复设定，**同一批物体在不同阶段呈现不同状态**：

| 阶段 | 调用 | 动画物体状态 | showObjs/hideObjs | 交互目标 |
|------|------|--------------|-------------------|----------|
| **Prepare** | `SetObjsActive` + `HideAnimations` + `OnPrepare` | **隐藏**（归位） | show 显 / hide 隐 | 子类隐藏（`OnPrepare`） |
| **Waiting** | `ShowAnimationsAtFirstFrame`（子类 Waiting 首行） | **显示 + 定格首帧** | 保持 | 子类显示（target/drag `SetActive(true)`） |
| **Complete** | `OnCompleteHide` + `PlayAnimations` + 等播完 + `HideAnimationsOnComplete` | **播放中 → 停在末帧 → 按需隐藏** | 保持 | 子类隐藏（`OnCompleteHide`） |
| **FastComplete（快进）** | `OnCompleteHide` + `StopAtLastFrame` + `HideAnimationsOnComplete` | **直接定格末帧**（不播放）→ 按需隐藏 | 保持 | 子类隐藏 |

> **观察**：`showObjs`/`hideObjs` 只在 **Prepare** 设定一次；**Waiting/Complete/快进** 都不再碰它。真正反复切换的是**动画物体**（隐藏↔首帧↔播放末帧↔定格末帧）和**子类交互目标**（隐藏↔显示↔隐藏）。这就是"状态保持"要管理的核心：**动画和交互物在同一步骤内、以及步骤之间，如何精确落位**。

#### (4) 步骤间的状态衔接：跳转 = 从 P0S0 全量重置（核心原则）

> **权威语义（参考 [`StepSystem_Manual.md`](StepSystem_Manual.md) §4.1）：所有跳转都从 P0S0 重置并快速执行到目标。** 这是为了保证动画状态一致性。跳转**中断当前协程（`StopAllCoroutines`），从第 0 进程第 0 步重新开始**，跳转代价与跳转距离成正比（距离越远，中间 FF 步骤越多）。

```
SetStep(2, 3)   ← 任意跳转（无论向前向后）
  → StopAllCoroutines（中断当前执行）
  → PrepareAllConditions()   全员归位：所有条件 Reset + Prepare → 隐藏所有动画物体
  → P0S0 → FF → P0S1 → FF → ... → P2S2 → FF → P2S3（正常执行）★目标
```

**三连语义**：

| 阶段 | 动作 | 动画物体状态 |
|------|------|--------------|
| ① 全员归位 | `PrepareAllConditions`（每个条件 `ResetCondition`+`Prepare`） | **全部隐藏**（`HideAnimations`），交互物隐藏，统一回到"未开始" |
| ② 目标前步骤快进 | `FastForward` → `FastComplete` | **从"隐藏"直接拨到"定格末帧"**（`StopAtLastFrame`），跳过 Waiting/不播放，呈现"已完成" |
| ③ 目标及之后步骤 | `ExecuteStep` 正常执行 | Waiting 时 `ShowAnimationsAtFirstFrame` **定格首帧**，等待交互 |

> **快进的本质**：在状态机上，把目标前每个步骤的动画状态**由"未开始（隐藏）"直接改写为"已完成（定格末帧）"**，并配合 `OnCompleteHide` 隐藏交互物。它跳过了"显示首帧 → 交互 → 播放动画"的中间态，直接落到终态。

**用你的例子验证（5 个步骤）：**

| 跳转 | 归位后动画状态 | 步骤 1 | 步骤 2 | 步骤 3 | 步骤 4 | 步骤 5 |
|------|----------------|--------|--------|--------|--------|--------|
| **1 → 3**（向前跳） | 全隐藏 | FF→**末帧** | FF→**末帧** | 正常→**首帧等待** | — | — |
| **3 → 2**（向后跳） | 全隐藏 | FF→**末帧** | 正常→**首帧等待** | 后续执行 | — | — |

- **1→3**：先全归位，再 1、2 快进到**末帧**（"已完成"），3 从**首帧**正常执行 ✓ 与你描述一致（1、2 的 Animation 停在最后一帧）。
- **3→2**：先全归位（注意：**归位是隐藏动画物体**，不是定格第一帧——只有目标步骤 2 的 Waiting 才定格首帧），再把 **1 快进到末帧**，2 正常执行（首帧等待交互）。

> ⚠️ **关键澄清**：归位阶段动画物体是**隐藏**（`HideAnimations`），**不是定格在第一帧**。"定格第一帧"只发生在目标步骤进入 **Waiting** 时（`ShowAnimationsAtFirstFrame`）。这一点与直觉不同，是状态保持一致性的关键。

#### (5) 各状态设置的调用时机（子类 Waiting 首行统一触发）

所有条件子类的 `Waiting()` 都在**开头**调用 `step.ShowAnimationsAtFirstFrame()`（定格首帧），然后各自显示交互目标并进入交互循环：

```csharp
// 例：ConditionClick.Waiting（其余子类同构）
target.gameObject.SetActive(true);     // ① 显示交互目标
step.ShowAnimationsAtFirstFrame();     // ② 动画定格首帧
// ③ 交互循环：订阅事件 → 等待命中 → 退订
```

**各子类 Waiting 对交互目标的显隐**：

| 条件 | Waiting 里显示 | `OnCompleteHide` 里隐藏 |
|------|----------------|--------------------------|
| `ConditionClick` | `target.SetActive(true)` | 隐藏 `targetObj` |
| `ConditionDrag` | `drag`+`target` 显示 | 隐藏 `dragObj`+`targetObj` |
| `ConditionTool` | 工具面板 | 关面板 |
| `ConditionUI` | 信息面板 | 关面板 |
| `ConditionQuestion` | 答题面板 | 关面板 |
| `ConditionLineConnect` | `ShowLineElements()` + 首帧 | `HideLineElements()` |
| `ConditionDefault` | 无（仅定格首帧） | 无 |

> ⚠️ **连线特例**：`ConditionLineConnect` **重写了 `FastComplete()`**（L44）：先 `step.Lines` 所有线模板 `SetActive(true)`（呈现"已连完"视觉），再走基类（`HideLineElements` + `StopAtLastFrame` + `HideAnimationsOnComplete`）。且连线**不动已提交的常驻连接线**（跳回后仍算完成，状态延续，与"步骤不保留状态"不同）。

#### (6) 正常完成 vs 快进的动画状态差异

| 对比项 | 正常完成（Complete） | 快进（FastComplete） |
|--------|----------------------|----------------------|
| 动画物体初态 | Waiting 定格首帧 | （跳过 Waiting） |
| `OnCompleteHide` | ✅ | ✅ |
| `PlayAnimations()` | ✅ 播放，等播完到末帧 | ❌ 不播放，`StopAtLastFrame` 直接定格末帧 |
| `HideAnimationsOnComplete` | ✅ | ✅ |
| 物体终态 | 动画停在末帧（或按 hideOnComplete 隐藏） | 动画瞬间定格末帧（或按 hideOnComplete 隐藏） |
| 关键差异 | 有**播放过程** | **无播放过程**，直接跳到末帧 |

### 5. 状态与打断

- **条件状态**：`StepStutus`（`Ready` 准备 / `Waiting` 等待 / `Complete` 完成），记录在 `condition.Status`。
- **协作式打断（关键设计）**：
  - `ForceComplete()`：置 `forceComplete=true` + `Status=Complete`。
  - 所有 `Waiting` 的阻塞点必须用 `WaitUntilOrForceComplete(predicate)`：`while(!predicate() && !forceComplete)` —— 因此 `ForceComplete` 能让 Waiting 协程**尽快退出**，而不是靠轮询条件。
  - `NextStep` / `SkipCurrentStep` / `CompleteCurrentStep` 都走 `condition.ForceComplete()`。

### 6. 事件驱动

**入口事件（`StepManager.DelayInit` 订阅，强引用 + `OnDestroy` 退订）**：

| 事件 | 触发动作 |
|------|----------|
| `StepNextRequestEvent` | `NextStep()`：完成当前步骤进入下一步 |
| `StepJumpRequestEvent` | `JumpToStep(stepIndex)`：当前进程内跳转 |
| `ProcessingJumpRequestEvent` | `JumpToStep(p, s)`：跳转进程/步骤 |

**状态发布事件（`StepManager` 执行过程中 `EventBus` 发布）**：

| 事件 | 时机 |
|------|------|
| `ProcessChangedEvent(p, handler)` | 每次进入新进程 |
| `StepPreparedEvent` | 步骤 Prepare 完成 |
| `StepWaitingEvent` | 步骤进入 Waiting |
| `StepCompletedEvent` | 步骤 Complete 完成 |
| `AllStepsCompletedEvent` | 全部完成 / Finish 步骤触发 |

### 7. 步骤导演流程（`StepManager`）

- **初始化**（`DelayInit`）：订阅入口事件 → 初始化所有步骤的条件（`ConditionInit`）→ 从第 0 进程第 0 步开始 `StartExecution()`。
- **正常执行**（`ExecuteAll` → `ExecuteStep`）：
  - 逐进程逐步骤执行三阶段，每步完成后发对应事件。
  - 进程间延迟 `processingDelayTime`(0.3s)、步骤间延迟 `stepDelayTime`(0.5s)。
  - **`Finish` 步骤特判**：不执行三阶段，直接置 `isFinished` + 发 `AllStepsCompletedEvent` 并结束（对齐 Tuanjie）。
  - 无条件步骤视为立即完成（连发三个事件）。
- **跳转**（`JumpToStep` → `JumpTo`）：全员归位（所有条件 `ResetCondition`+`Prepare`）→ 目标前步骤 `FastForward`（动画跳末帧）→ 目标及之后正常执行 → 继续推进。
- **上一步**（`PrevStep`）：同进程上一步 / 上一进程最后一步（走 `JumpToStep`）。
- **停止**（`StopExecution`）：停止协程，lifecycle 复位 `Idle`。

**生命周期状态**：`StepLifecycle`（`Idle`/`Prepare`/`Waiting`/`Complete`），`IsRunning` 表示非 Idle。

### 8. 动画控制（`StepHandler`，Legacy Animation 精确控帧）

| 方法 | 作用 |
|------|------|
| `SetObjsActive()` | 显示 showObjs、隐藏 hideObjs |
| `HideAnimations()` | 隐藏所有动画物体（Prepare 归位） |
| `ShowAnimationsAtFirstFrame()` | 显示动画物体并停第一帧（Waiting 进入时） |
| `PlayAnimations()` | 播放所有动画（Complete 阶段） |
| `StopAtLastFrame()` | 动画瞬间跳末帧（FastComplete 用） |
| `AnyAnimationPlaying()` | 是否还有动画在播放（等播完用） |
| `HideAnimationsOnComplete()` | 按 hideOnComplete 隐藏动画物体 |

> ⚠️ `StepAnimation.clip` 必须是**非循环动画**，否则 `Complete` 阶段 `WaitUntil(!AnyAnimationPlaying())` 永不结束。

### 9. 各条件子类 Waiting 实现要点

- **`ConditionClick`**：`OnPrepare` 隐藏 target → Waiting 显示 target + 动画首帧，订阅全局交互，命中 `Click + target` 即完成。
- **`ConditionDrag`**：`OnPrepare` 隐藏 drag/target → Waiting 按下 drag（`Down` 事件）起拖（隐藏源）→ 松开（查左键状态，不能靠 Up 事件）→ 手动射线 `RaycastHitTarget(target)` 命中即成功，否则恢复源物体重试。
- **`ConditionTool`**：通过 `GlobalControllerMgr.Find("StepToolPanelController")` 找 `IStepToolPanel` 契约；`ShowPanel` → 选工具(`OnToolPressed`，匹配 `usingId`) → `SetToolDragging(usingId)` 拖拽 → 松开查 `RaycastHitTarget(targetObj)` 命中即成功；面板未实现则告警跳过。
- **`ConditionUI`**：通过 `GlobalControllerMgr.Find("StepUiPanelController")` 找 `IStepUiPanel` 契约；`ShowData(usingId)` 弹信息面板，订阅 `OnPanelClosed` 关闭即完成；面板未实现则告警跳过。
- **`ConditionQuestion`**：通过 `GlobalControllerMgr.Find("StepQuestionPanelController")` 找 `IStepQuestionPanel` 契约，`ShowQuestion(usingId)`，订阅 `OnQuestionCorrect` 答对即完成；找不到面板则告警跳过。
- **`ConditionLineConnect`**：见「任务四」连线周期；轮询 `ElementManagerBase` 已连接线，模板端点对全匹配即完成；`OnPrepare`/跳转归位时 `CancelDrawing` 取消临时线，不销毁已提交常驻线（跳回后仍算完成）；`FastComplete` 重写为先显示线模板再走基类（见 4.1）。
- **`ConditionFinish`**：`StepManager.ExecuteStep` 特判直接结束，不执行三阶段。

> 详细手册见本项目的 [`StepSystem_Manual.md`](StepSystem_Manual.md)（已按当前 `MCV_Module_0802` 架构移植适配）。

---

## 六、实现状态汇总

| 环节 | 状态 | 关键代码 | 待办 |
|------|------|----------|------|
| 事件导航链路 | 🟡 部分 | `TaskListPanel`→`GlobalUIMgr`/`TaskListController` | `LoginSuccessEvent` 无监听、Menu 叶子无发布、`SceneSwitchRequestEvent` 无发布方 |
| Setup | ✅ 已实现 | `Setup.cs` | — |
| Start | 🟡 骨架 | `StartController`/`StartPanel` | 视频播放、跳转按钮 |
| Login | 🟡 部分 | `LoginController`/`LoginPanel` | `VerifyLogin` 占位、登出未实现 |
| Menu | 🟡 部分 | `MenuController`/`MenuPanel` | 叶子菜单进入 Task 未接入、需过滤 `TaskActive` |
| 任务一 Purpose | ✅ 基础 | `TaskPurposePanel` | — |
| 任务二 Equipment | 🟡 部分 | `TaskEquipmentPanel` | RawImage Shader/alpha 通道 |
| 任务三 Principle | ✅ 基础 | `TaskPrinciplePanel` | — |
| 任务四 LineConnection | 🟡 部分 | `ConditionLineConnect`/`LineDraw`/`ElementManagerBase` | 面板占位、2D 连线未处理 |
| 任务五 Training | 🟡 占位 | `TaskTrainingPanel` | 仿真实验流程 |
| 任务六 Test | 🟡 占位 | `TaskTestPanel` | 答题交互流程 |
| 步骤逻辑 | 🟡 框架已建 | `StepHandler`/`ProcessingHandler`/`ConditionBase` 系列 | 手册整理 |

> 图例：✅ 完整 / 🟡 骨架或部分 / ⬜ 未实现
