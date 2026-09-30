# Utils/Pool —— 通用 GameObject 对象池工具（纯工具层）

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：无

> 路径：`Assets/Scripts/Utils/Pool/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Utils.Pool`

把「按预制体实例化 → 复用 → 归还 → 销毁」从业务里抽出来的纯工具层，供器件模型展示、UI 列表项、连线临时物体等高频实例化场景复用。

## 选文件

- `IPoolable.cs` — 池化回调契约（`OnSpawn` / `OnDespawn`）
- `PooledObject.cs` — 池内实例归属标记（由池自动挂载，勿手工增删）
- `GameObjectPool.cs` — 单个预制体的池：取用 / 归还 / 预热 / 销毁
- `ObjectPoolMgr.cs` — 多池注册表（key → `GameObjectPool`）

## 跨文件约定

- 本层不依赖任何管理器：实例化工厂（`PoolInstantiateFunc`）由使用方注入；本工程预制体统一走 `GlobalAssetsMgr.LoadPrefabAsync` → `GlobalAddressableMgr`（由它登记「实例 → 包配置 id」）。
- 谁用谁持有池注册表：`InstManagerBase` 每管理器一个、`GlobalAssetsMgr` 一个全局共享；本目录不是全局管理器、不进 `Setup` 等待链。
- 新增使用点优先走 `GlobalAssetsMgr.SpawnPrefabAsync` / `DespawnPrefab` 门面，不要自己 new 池（除非像 `InstManagerBase` 那样需要独立生命周期）。
- 资源包卸载时可能连带销毁池内实例（`GlobalAddressableMgr.DestroyInstances`），不要长期缓存非活跃实例的引用。
