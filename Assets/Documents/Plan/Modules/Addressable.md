# Addressable 资源管理

## 概述

统一资源加载系统，通过 `GlobalAddressableMgr`（第 7 个 GlobalManager）提供单一入口 `LoadAsset<T>`。底层按 `PackageType` 分发到三种策略：`Default`（Resources）、`AA`（Unity Addressables）、`AB`（AssetBundle）。调用方无需感知策略差异。

## 三种策略

| 策略 | PackageType | 内容 | 加载方式 | 配置 |
|------|:--:|------|------|------|
| Resources | `Default` | 本包内置资源（含 0_Setup、1_Controller、2_UI、99_Loading 场景） | `Resources.Load` | 零配置 |
| Addressables | `AA` | 实验场景（N_xxx） | `Addressables.LoadAssetAsync` | Addressables Group |
| AssetBundle | `AB` | 模型、预制体、图片 | `AssetBundle.LoadFromFile` | 独立打包 |

## 加载 API

```csharp
GlobalAddressableMgr.Instance.LoadAsset<T>(
    string key,
    Action<T> onSuccess,
    Action<string> onFailure
);
```

- `key`：资源唯一标识，与 PackageConfigSO.key 对应
- `onSuccess`：加载成功回调
- `onFailure`：加载失败回调
- 异步回调式，不阻塞主线程

## 加载流程

```
LoadAsset<T>(key, onSuccess, onFailure):
    config = PackageDatabaseSO.Find(key)
    switch config.type:
        case Default → Resources.Load<T>(config.path)
        case AA      → Addressables.LoadAssetAsync<T>(config.path)
        case AB      → AssetBundle.LoadFromFile → LoadAsset<T>
```

## PackageConfigSO

`PackageConfigSO` 为单个资源的 ScriptableObject 配置，`PackageDatabaseSO` 为管理所有 `PackageConfigSO` 清单的数据库（内部字典，`Find(key)` 返回对应 `PackageConfigSO`）。

| 字段 | 说明 |
|------|------|
| `key` | 资源唯一标识，`PackageDatabaseSO.Find(key)` 的查找键 |
| `type` | PackageType（Default/AA/AB） |
| `path` | AA=Addressables key / AB=包内路径 / Default=Resources 相对路径 |

去 version/dependencies，以增加包尺寸代价换配置简单性。

## 热更新与降级

- 热更新短期不考虑，所有资源本地打包
- 加载失败不降级（不尝试备用策略），直接回调 `onFailure`

→ [设计决策：Why-Addressable](../Decisions/Why-Addressable.md)
