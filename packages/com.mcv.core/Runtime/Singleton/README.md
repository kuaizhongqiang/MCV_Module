# Singleton —— 框架级单例与生命周期基座（不含任何业务逻辑）

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：被 `Managers/`（13 个 `Global*Mgr`、`ElementManagerBase`、`StepManager`、`InstManagerBase`）继承；`Setup.cs` 经 `WaitForInit<T>()` 轮询 `IsInit` 完成启动编排

> 路径：`Assets/Scripts/Singleton/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Singleton`

## 选文件
- `SingletonBase.cs` — 生命周期基类：只给 `isInit` 与 `DelayInit`
- `SingletonGlobalMgr.cs` — 全局管理器泛型单例：唯一实例 + 常驻

## 跨文件约定
- **`isInit` 必须手动置位**：`SingletonBase.Start()` 只启动协程，子类 `DelayInit()` 结束时要 `isInit = true;`，否则 `Setup` 会等满超时时间。
- **每个具体类型各自持有静态实例**：静态字段按封闭泛型隔离，`FindObjectsByType<T>` 用具体类型查找，不会串实例。
- **退出态保护**：退出 / 销毁后访问 `Instance` 会打警告并返回 `null`；可能晚于管理器销毁的调用方请用 `SafeInstance` / `Exists` 判空。
- **依赖顺序**：子类里跨管理器调用（如 `GlobalSceneMgr` 用 `GlobalAddressableMgr`）写在 `DelayInit` 中并自行等待对方 `IsInit` / `Exists`，不要依赖 `Awake` 顺序。
