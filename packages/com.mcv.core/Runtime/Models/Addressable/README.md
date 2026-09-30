# Models/Addressable —— 资源加载 / 打包的配置数据：`GlobalAddressableMgr` 与编辑器打包工具共同的数据契约

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`Managers/GlobalAddressableMgr`（消费配置）、`Managers/GlobalSceneMgr`（AA 路由）、`Assets/Editor/BuildTools/ContentBundleTools` + `ContentProviders/*`（内容 AB 流水线）、`Assets/Editor/BuildTools/CameraBgBundleTools`（CameraBg 背景图 AB）、`Docs/design_ai/BundlePipeline.md`（完整规约）

> 路径：`Assets/Scripts/Models/Addressable/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Models.Addressable`

## 选文件
- `AddressableData.cs` — 加载策略枚举：Default / AA / AB 三条链路
- `PackageConfigSO.cs` — 包配置抽象基类：`PackageType` + `GetLoadKey()`
- `AAPackageConfigSO.cs` — AA 包配置：address / labels / groupName
- `ABPackageConfigSO.cs` — AB 包配置：clipId / bundleName / assetPath
- `DefaultPackageConfigSO.cs` — 默认直引用包配置：load key = id
- `PackageDatabaseSO.cs` — 包配置总清单：查找 / 校验 / 自动收集
- `SceneAddressableConfig.cs` — 场景级 AA 路由表（放 `Resources/`）
- `ContentNaming.cs` — 内容 AB 命名规则的唯一来源
- `ContentAssetKind.cs` — 内容资源类型：决定加载泛型（必须准确）

## 跨文件约定
- **新增一类内容 = 一个 `IContentProvider` 实现 + 登记进 `ContentBundleTools.Providers`**；打包与命名规约见 `Docs/design_ai/BundlePipeline.md`。
