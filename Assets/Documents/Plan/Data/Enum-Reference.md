# 枚举速查

## 概述

项目中关键枚举的快速参考。新增枚举时同步更新此文档。

## TaskType

命名空间：`MCV_Module.Data.Project`

| 值 | 中文 | 数据类别 | 驱动系统 | 说明 |
|------|------|:--:|------|------|
| `None` | 无 | — | — | 默认空值 |
| `Purpose` | 任务目的 | 图文 | UI 展示 (MCV) | 实验背景与目标 |
| `Equipment` | 实验仪器 | 图文 | UI 展示 (MCV) | 仪器介绍 |
| `Principle` | 实验原理 | 图文 | UI 展示 (MCV) | 理论知识 |
| `LineConnection` | 电路连接 | 步骤 | StepSystem + LineConnect | 3D 端点配对连线 |
| `Training` | 仿真实验 | 漫游+步骤 | FPS 漫游 + StepSystem | 综合实操训练 |
| `Test` | 小测验 | 图文 | UI 展示 (MCV) | 课后考核 |

## PackageType

命名空间：`MCV_Module.Data.Addressable`

| 值 | 中文 | 加载方式 | 管理内容 |
|------|------|---------|------|
| `Default` | 本包资源 | `Resources.Load` | 启动必备、UI 基础资源 |
| `AA` | Addressables | `Addressables.LoadAssetAsync` | 实验场景 |
| `AB` | AssetBundle | `AssetBundle.LoadFromFile` | 模型、预制体、图片 |

## 选择指南

| 场景 | 推荐 PackageType |
|------|:--:|
| 启动必备资源 | `Default` |
| 实验场景（N_xxx） | `AA` |
| 实验专用模型/图片 | `AB` |
| 通用 UI 精灵 | `Default` |

## 新增枚举规则

1. 确定命名空间（Data / StepSystem / UI）
2. 包含 `None` 零值作为默认
3. 更新本文档
4. 更新关联文档（JSON Schema / ProjectClip / UI 框架）

→ [JSON Schema](JSON-Schema.md) | [数据层](../Architecture/Data-Layer.md)
