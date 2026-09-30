# Managers —— 全局管理器：单例 + 静态入口 + 事件总线

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`../README.md`（依赖方向）；`../Singleton/README.md`、`../Event/README.md`

> 路径：`Assets/Scripts/Managers/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Managers[.*]`

服务层：向 Controller / UI / 场景对象提供无耦合的全局能力，全部派生自 `Singleton/SingletonBase`（多数为 `SingletonGlobalMgr<T>`）。

## 选文件

- `GlobalDataMgr.cs` — 全局数据容器；当前器件/任务类型/画质唯一读写点
- `GlobalUIMgr.cs` — Canvas 注册表 + 状态事件驱动重建
- `GlobalSceneMgr.cs` — 场景加载/卸载唯一入口 + 退出收口
- `GlobalControllerMgr.cs` — Controller 注册表（UI 与 Controller 解耦）
- `GlobalInteractiveMgr.cs` — 逐帧射线交互中枢与全局交互事件
- `GlobalAddressableMgr.cs` — 包配置注册表；AA/AB/Default 资源装卸唯一入口
- `GlobalAiMgr.cs` — AI 总开关：建客户端/进程、会话、预热
- `GlobalAssetsMgr.cs` — 资源门面：图片 LRU、预制体池、一 clip 一 AB 装卸
- `GlobalAudioMgr.cs` — 音频中枢（BGM/语音/音效）+ 音量渐变
- `GlobalCameraMgr.cs` — 主相机与 CinemachineBrain/CameraBg 同步
- `GlobalInputMgr.cs` — 输入控制器注册表 + 鼠标静止/移动判定
- `AiServerProcess.cs` — AiServer EXE 进程宿主（拉起与优雅关闭）

## 跨文件约定

- **`DelayInit` 必须置 `isInit = true`**，否则 `Setup` 白等 15 s（超时仅告警）。无异步依赖的直接置位（Assets / Controller / Input），其余等数据或配置就绪。
- **订阅与退订成对**：`DelayInit` 里 `EventBus<...>.Subscribe`，`OnDestroy` 里对应 `Unsubscribe`。
- **`SceneState` / `TaskType` 一律事件驱动，禁止参数透传与拉取查询**：写方只 `Publish`；读方订阅并缓存。`SceneState` 无数据层副本，**不提供 `GetCurrentState()`**；跨 Canvas 重建的方法一律不带 `state` / `taskType` 参数。
- **对外优先暴露静态方法**（`GlobalDataMgr.GetTaskData(...)`），不让调用方持有 `Instance`；退出阶段用 `Exists` / `SafeInstance`。管理器横向调用要判空/等就绪，别在 `Awake` 里假设对方已初始化。
- **新增管理器**：继承 `SingletonGlobalMgr<T>` → 实现 `DelayInit` → 启动必需则加入 `Setup` 等待列表。

## 与其他目录的关系

- 上游：`Setup.cs`（启动编排）、`Singleton/`（基类）、`Event/`（订阅）、`Models/`（数据）、`Interfaces/`（契约）。
- 下游：`Controllers/`（调度层读数据、切场景、查面板）、`UI/`（Canvas 注册与重建）、`Objects/`（交互注册）、`Steps/`（步骤导演）。
- 子目录：`Steps/`（步骤导演）、`InstManagers/`（场景实例对象管理器）、`RoomDynamic/`（房间动态管理器基类；业务实现 `RoomOneDynamicMgr` 已随业务移除）。
