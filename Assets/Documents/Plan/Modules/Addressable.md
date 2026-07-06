# Addressable 资源管理

## 概述

统一资源加载系统，支持三种策略（`PackageType` 枚举）：`Default`（Resources）、`AA`（Unity Addressables）、`AB`（AssetBundle）。通过 `PackageConfigSO`（ScriptableObject）配置单个资源包，`PackageDatabaseSO` 管理所有包清单，`PackageDataRepository` 为运行时仓储。

## 待完善

- 三种加载策略的切换与降级
- `PackageConfigSO` 字段详解
- `GlobalAddressableMgr` 加载 API
