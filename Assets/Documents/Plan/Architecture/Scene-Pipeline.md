# 场景加载管线

## 概述

运行时场景采用 **异步 Additive 加载** 模式，通过 `GlobalSceneMgr` 统一管理。中间使用 `99_Loading` 场景 + LoadingCanvas 遮挡过渡，由 `GlobalSceneMgr` 统一驱动进度。

```
0_Setup → 1_Controller + 2_UI + N（三维实验场景）
           └── 99_Loading（过渡遮挡）
```

## 加载流程

### 阶段 1：0_Setup 启动

`Setup.cs` 顺序初始化 9 个 GlobalManager，每个 `DelayInit()` 完成后标记 `isInit = true`，按序推进：

```
AudioMgr → ControllerMgr → DataMgr → InteractiveMgr → SceneMgr → UiMgr → AddressableMgr → CameraMgr → StepSystemMgr
```

初始化失败策略：重试 + 超时 → `Application.Quit()`。

### 阶段 2：加载常驻场景

Setup 完成后按序异步加载：
1. `1_Controller`：Controller 注册与生命周期管理
2. `2_UI`：常驻 UI 层，CanvasBase 层级管理，Panel 栈管理

### 阶段 3：按需加载实验场景

用户选择实验后加载对应 `N_xxx` 场景：
```
GlobalSceneMgr 发命令 → GlobalAddressableMgr.LoadAsset<Scene>(key, onSuccess, onFailure)
  → AA 策略异步加载场景 → 回调 onSuccess → SceneMgr 激活场景
```

### 阶段 4：切换实验

`1_Controller` 和 `2_UI` 始终保留不卸载：
1. 显示 `99_Loading` + LoadingCanvas 遮挡
2. 卸载旧 N 场景（`SceneManager.UnloadSceneAsync`）
3. 加载新 N 场景
4. 隐藏 `99_Loading`

步骤系统内部 P0S0 快进跳转同样走 `99_Loading` 遮挡流程。

## 场景命名规范

| 场景 | 编号 | 加载方式 | 说明 |
|------|:--:|------|------|
| `0_Setup` | 0 | Default（内置） | 启动入口 |
| `1_Controller` | 1 | Default（内置） | Controller 层 |
| `2_UI` | 2 | Default（内置） | 常驻 UI 层 |
| `N_xxx` | ≥3 | AA（Addressables） | 实验三维场景，按需加载卸载 |
| `99_Loading` | 99 | Default（内置） | 加载过渡遮挡 |

## 加载实现

- 加载方式：`SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive)`
- 进度驱动：`asyncOperation.progress` → LoadingCanvas 进度条
- 场景激活：`asyncOperation.allowSceneActivation = false` → 加载到 90% 时等待 → 手动 `allowSceneActivation = true` 激活
- 卸载：`SceneManager.UnloadSceneAsync(name)`

## 与 AddressableMgr 关系

```
GlobalSceneMgr → 发布加载命令 → GlobalAddressableMgr → 加载场景 Asset → 回调 → SceneMgr 管理生命周期
```

- 内置场景（0/1/2/99）：`SceneManager.LoadSceneAsync` 直接加载
- 实验场景（N_xxx）：通过 `GlobalAddressableMgr.LoadAsset<Scene>` 走 AA 策略

→ [Addressable 资源管理](../Modules/Addressable.md)
