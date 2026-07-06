# 全局管理器

## 概述

8 个全局管理器均继承 `SingletonGlobalMgr<T>`，以 `Instance` 静态属性访问，`DontDestroyOnLoad` 跨场景持留。各管理器通过 `DelayInit()` 协程完成初始化。初始化顺序由 `Setup.cs` 控制：AudioMgr → ControllerMgr → DataMgr → InteractiveMgr → SceneMgr → UiMgr → AddressableMgr → CameraMgr。

## 管理器清单

| 管理器 | 职责 |
|--------|------|
| `GlobalAudioMgr` | 音频播放控制 |
| `GlobalControllerMgr` | Controller 注册与查找 |
| `GlobalDataMgr` | 持有 System/Project/User 数据，JSON 读写 |
| `GlobalInteractiveMgr` | 交互物体注册与 Raycast 检测 |
| `GlobalSceneMgr` | 场景加载/卸载 |
| `GlobalUiMgr` | Canvas/Panel 注册与查找 |
| `GlobalAddressableMgr` | AA/AB/Resources 统一资源加载 |
| `GlobalCameraMgr` | 相机管理 |

## 待完善

- 各管理器的详细 API 文档
- 初始化依赖与失败处理
