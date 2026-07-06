# ADR: 为什么用 Addressable 管理资源

## 概述

记录选择 Unity Addressables 配合 AssetBundle 和 Resources 三种策略共存的决策理由，以及 `PackageType` 枚举的设计初衷。

## 待完善

- 备选方案（纯 Resources、纯 AssetBundle、纯 Addressables）
- 三种策略的选择依据
- 热更新与分包策略
