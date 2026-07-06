# 全局管理器

## 概述

9 个全局管理器均继承 `SingletonGlobalMgr<T>`，以 `Instance` 静态属性访问，`DontDestroyOnLoad` 跨场景持留。各管理器通过 `DelayInit()` 协程完成初始化。

**初始化顺序**（由 `Setup.cs` 控制）：AudioMgr → ControllerMgr → DataMgr → InteractiveMgr → SceneMgr → UiMgr → AddressableMgr → CameraMgr → StepSystemMgr。

初始化失败策略：重试 + 超时 → `Application.Quit()`。`Setup.cs` 通过 `IsGlobalMgrInit<T>()` 检查 `isInit` 标记推进初始化链。

## 管理器清单

| # | 管理器 | 职责 | 关键 API |
|:--:|------|------|---------|
| 1 | `GlobalAudioMgr` | 音频播放控制 | Play/Stop/SetVolume/Pause |
| 2 | `GlobalControllerMgr` | Controller 注册与查找 | Register(c) / GetController(id) / Unregister(c) |
| 3 | `GlobalDataMgr` | 数据持有与 JSON 读写 | Read\<T\>(file) / Write\<T\>(file, data) |
| 4 | `GlobalInteractiveMgr` | 交互物体注册 + Update 统一射线检测 | Register(obj) / Unregister(obj) |
| 5 | `GlobalSceneMgr` | 场景异步加载/卸载，驱动 LoadingCanvas | LoadScene(name) / UnloadScene(name) / SwitchExperiment(clipId) |
| 6 | `GlobalUiMgr` | Canvas/Panel 注册查找 + 栈管理 | GetCanvas\<T\>() / GetPanel\<T\>() / RegisterPanel(p) |
| 7 | `GlobalAddressableMgr` | AA/AB/Default 统一资源加载 | LoadAsset\<T\>(key, onSuccess, onFailure) |
| 8 | `GlobalCameraMgr` | 相机管理与切换 | SetActiveCamera(cam) / GetMainCamera() |
| 9 | `GlobalStepSystemMgr` | StepDirector 管理、步骤生命周期入口 | 待定（Step 系统后设计） |

## SingletonGlobalMgr\<T\> 基类

所有管理器继承同一个泛型基类，提供：

- **线程安全**：`lock` 保护单例创建
- **DontDestroyOnLoad**：跨场景持留
- **Quit 保护**：`Application.Quit()` 后不再创建实例
- **DelayInit() 协程**：初始化逻辑入口，完成后 `isInit = true`
- **Instance 属性**：全局唯一访问点

```csharp
public class GlobalDataMgr : SingletonGlobalMgr<GlobalDataMgr>
{
    protected GlobalDataMgr() { }  // 防外部 new

    protected override IEnumerator DelayInit()
    {
        SystemData = JsonReaderWriter.Read<SystemData>("SystemData");
        yield break;
    }
}
```

## 依赖关系

所有 Manager **平级互调**，无方向限制，各管一摊，无调用环。没有硬性依赖顺序——当前顺序是习惯性排列。

运行时调用示例：
- `GlobalInteractiveMgr` 射线命中 → 调 `GlobalUiMgr` 打开 Panel
- `GlobalSceneMgr` 加载场景 → 调 `GlobalAddressableMgr` 加载资源
- `StepCondition` Waiting 阶段 → 调 `GlobalAudioMgr` 播配音

## Setup.cs 初始化推进

```csharp
IEnumerator Start()
{
    yield return StartCoroutine(WaitForInit<GlobalAudioMgr>());
    yield return StartCoroutine(WaitForInit<GlobalControllerMgr>());
    // ... 依次推进
    yield return StartCoroutine(WaitForInit<GlobalStepSystemMgr>());
    
    // 全部就绪，跳转场景
    GlobalSceneMgr.Instance.LoadMainScene();
}

IEnumerator WaitForInit<T>() where T : SingletonGlobalMgr<T>
{
    int retry = 0;
    while (!IsGlobalMgrInit<T>() && retry < MAX_RETRY)
    {
        yield return new WaitForSeconds(RETRY_DELAY);
        retry++;
    }
    if (retry >= MAX_RETRY)
        Application.Quit();
}
```

## 新增 Manager 规则

1. 继承 `SingletonGlobalMgr<T>`
2. 提供 `protected` 无参构造函数
3. 初始化逻辑写入 `DelayInit()` 协程
4. 在 `Setup.cs` 中注册到初始化链
5. 通过 `Instance` 供全局访问

→ [编码规范](../Guides/Coding-Conventions.md)
