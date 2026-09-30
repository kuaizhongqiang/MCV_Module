# Managers/InstManagers —— 按包 id 取用/归还场景实例（池化）与简化步骤系统

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`../README.md`（资源管线与数据来源）

> 路径：`Assets/Scripts/Managers/InstManagers/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Managers.InstManagers`

两类用途：`InstManagerBase` / `InstShowManager` 是「按包配置 id 取用/归还实例」的池化管理器（预制体经 `GlobalAssetsMgr` → `GlobalAddressableMgr` 包管线加载，实例走 `Utils/Pool`）；`InstControlledManager` 不做池化，是「点击 + 播动画」的简化步骤系统。

## 选文件

- `InstManagerBase.cs` — 实例管理器基类：按包 id 加载预制体并池化取用/归还
- `InstShowManager.cs` — 仅展示型实例管理器，实例只看不参与步骤交互
- `InspectionManager.cs` — 检测任务装配：按当前 clip 装检测预制体，切任务销毁
- `InstControlledManager.cs` — 简化步骤系统：结构模型预制体上的线性「点击 + 播动画」

## 跨文件约定

- **不参与启动链**：池化管理器不在 `Setup` 等待的 11 个管理器内，`DelayInit` 只在自身被实例化时跑，因此基类**自行等待** `GlobalAddressableMgr.IsInit`（超时 `maxWaitTime`）；`InstControlledManager` 是普通 `MonoBehaviour`，无 `DelayInit`，初始化只发生在 `StartSteps` 被调用时。
- **同步 `Spawn` 的前提是包已预加载**（`packageKeys` 填好并等 `IsReady`）；运行时才知道的包用 `SpawnAsync`（`InstShowManager` 是 `AllowEmptyPackageKeys = true` 的按需型，包由业务首次 `SpawnAsync` 注入）。
- **检测 / 结构模型都不走对象池**：`InspectionManager` 与 `TaskStructureController`（Controllers）都是「进页 `Instantiate`、收口销毁」—— 预制体内部会按交互改状态（表笔位置、零件 transform），池化复用会带回上一次状态。实例仍经 `GlobalAddressableMgr.InstantiatePrefab` 登记包归属，所以「离开内容页」这条收口只能放开引用、不能自己 `Destroy`（卸包会连带销毁）。
- **卸载资源包会连带销毁实例**（含池内闲置），故不要长期缓存非活跃实例引用；只想重置请用 `DespawnAll()`。管理器销毁时池 `Dispose`（闲置/活跃一并销毁）。
- **宿主默认关闭的用法（`InstShowManager`）**：挂在默认关闭的 `ShowObjParent` 上（其下两台展示渲染相机有出图开销），非激活物体收不到 `Awake`/`Start`，业务必须先 `SetShowRootActive(true)`；**开必须配关**，收口两条路径都要走 —— 切到非 Info 任务（`TaskTypeChangeEventData`）与离开内容页（`SceneStateChangeEventData` 非 `UI`）；后者不能省，离开内容页不发任务类型事件，漏了会让展示相机一直激活。
- **`InstControlledManager` 的坑**：两套入口（`StartSteps` / `PlayAuto`）都靠外部发起且互斥，都先 `StopSteps()`；只有手动流程发 `StructInteractiveCompletedEvent`（自动来回播会反复计分，故只回调）；`speed` 正负同时决定方向与步序（正 = 0→N + 正放，负 = N→0 + 每段倒放）；`clickObj` 的 `SetInteractable` 必须连碰撞体一起关（`GlobalInteractiveMgr` 取最前面碰撞体再判 `IsInteractable`，只关标志会让前方零件挡住射线，表现为「点不到」），而 `HoverOnly` 必须留着碰撞体；`clip` 必须非循环，`animSpeed` / `speed` 为 0 会当 1。详见 `InstControlledManager.md`。

## 与其他目录的关系

- 基类 / 池：`Singleton/SingletonBase.cs`、`Utils/Pool/`（`ObjectPoolMgr` / `GameObjectPool` / `IPoolable`）。
- 资源管线：`../GlobalAssetsMgr`（`LoadPrefabAsync`）→ `../GlobalAddressableMgr`（`LoadAssetAsync` / `InstantiatePrefab` / `DestroyInstances`）。
- 数据来源：`Models/Project/ProjectData` 的 `prefabKey`（**包配置 id，不是资源路径**）。
- `InstControlledManager` 还依赖：`Objects/Interactives/InteractiveBase`、`Event/CoreEvent.cs`（`GlobalInteractionEventData`）、`Event/EventBus.cs` + `StructInteractiveCompletedEvent`；不支持拖拽/工具/面板/答题/连线，需要这些请用 `Managers/Steps/StepManager`。
