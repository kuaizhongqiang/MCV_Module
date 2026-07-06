# ADR: 为什么用 Addressable 管理资源

> 状态：已采纳 | 日期：2026-07-06

## 决策

采用 **Unity Addressables + 传统 AssetBundle + Resources** 三种策略共存，通过 `PackageType` 枚举（`Default` / `AA` / `AB`）按资源类型分工管理。

## 分工策略

| 策略 | PackageType | 管理内容 | 原因 |
|------|:--:|------|------|
| **Addressables** | `AA` | 实验场景（N_xxx） | 异步加载、内置进度回调、适合大体积按需场景 |
| **AssetBundle** | `AB` | 模型、预制体、图片 | 手动打包、包体积可控、与 AA 互补 |
| **Resources** | `Default` | 本包内置资源（含 0_Setup、1_Controller、2_UI、99_Loading 场景） | 零配置、适合启动必备资源和常驻场景 |

## 背景

项目需要按需加载实验场景和资源，避免一次性加载导致内存峰值。场景和资源包需要独立管理，加载 API 统一。

## 备选方案

| 方案 | 优点 | 缺点 |
|------|------|------|
| 纯 Resources | 简单 | 全量常驻内存，无法按需释放 |
| 纯 AssetBundle | 可控 | 需手写加载/卸载/依赖管理，开发成本高 |
| 纯 Addressables | 自动化 | 对非场景资源粒度控制不够细 |
| **三种共存** | 各取所长 | 需维护 `PackageType` 分发逻辑 |

## 加载流程

```
GlobalSceneMgr 发命令 → GlobalAddressableMgr 加载资产 → SceneMgr 管理生命周期
```

API：`LoadAsset<T>(string key, Action<T> onSuccess, Action<string> onFailure)`，回调式异步。

## 策略选择

- 热更新：**短期不考虑**
- 加载失败降级：暂不实现，后续按需
- PackageConfigSO：**去 version / dependencies**，以增加包尺寸代价换配置简单性

## 后果

- 三种加载策略共存，需在 `GlobalAddressableMgr` 中维护 `PackageType` 到加载逻辑的分发
- AB 包需额外打包流程，但换来更细的粒度控制
