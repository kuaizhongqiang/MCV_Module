# Event —— 全局发布 / 订阅基础设施：`EventBus<T>` 一类型一条总线，其余文件集中定义全部事件载荷（DTO）

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：无（事件层被 `Managers` / `Controllers` / `UI` / `Objects` / `Steps` 引用，自身不依赖任何业务层）

> 路径：`Assets/Scripts/Event/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Event`

## 选文件
- `EventBus.cs` — 类型安全静态总线：一类型一条订阅表
- `CoreEvent.cs` — core 域载荷：音频 / 相机 / 场景 / 内容包 / 交互 / 步骤控制等
- `CoreEventModule.cs` — module 域载荷：引用元件 / 步骤类型的事件
- `DialogEventDispatcher.cs` — 对话框集中分发：固定链路弹 `DialogPanel`

## 跨文件约定
- **强引用订阅**：`EventBus` 持强引用，订阅者销毁时未 `Unsubscribe` 会导致误回调；所有订阅点都必须有配对退订（`OnDestroy` 或 `ResetCondition` 之类兜底）。
- **载荷归属**：只涉及引擎 / 框架基础类型的载荷放 `CoreEvent.cs`；一旦引用 `Objects` / `Steps` 域类型，必须放 `CoreEventModule.cs`。
- **`GlobalInteractionEventData` 走对象池**：使用后需 `Release`，不要长期缓存实例。
- **禁止在事件载荷里写逻辑**：本目录只放数据结构与总线实现。
