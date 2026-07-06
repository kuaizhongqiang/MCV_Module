# 数据层

## 概述

所有数据类继承 `DataBase`（提供 `id` 和 `displayName`），组织在 `MCV_Module.Data.*` 命名空间下。`GlobalDataMgr` 持有三大数据根对象：`SystemData`（系统配置）、`ProjectData`（实验片段列表）、`UserData`（用户信息）。数据通过 `JsonReaderWriter`（Newtonsoft.Json）读写 `StreamingAssets/Data/` 下的 JSON 文件。

## 待完善

- `DataBase` 继承链全图
- `[Serializable]` / `[SerializeField]` / `[NonSerialized]` 使用规范
- JSON 序列化配置与容错策略
