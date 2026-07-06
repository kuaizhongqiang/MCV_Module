# ProjectClip 数据规范

## 概述

`ProjectClip`（实训项目）是核心业务实体，继承 `DataBase`。每个 Clip 包含若干 Task（实训任务），按 TaskType 分为 6 种类型，归入三类数据。

## TaskType 与数据类别

| TaskType | Data 类 | 数据类别 | 驱动系统 |
|----------|---------|:--:|------|
| `Purpose` | `TaskPurposeData` | 图文 | UI 展示 (MCV) |
| `Equipment` | `TaskEquipmentData` | 图文 | UI 展示 (MCV) |
| `Principle` | `TaskPrincipleData` | 图文 | UI 展示 (MCV) |
| `LineConnection` | `TaskLineConnectionData` | 步骤 | StepSystem + LineConnect |
| `Training` | `TaskTrainingData` | 漫游+步骤 | FPS + StepSystem |
| `Test` | `TaskTestData` | 图文 | UI 展示 (MCV) |

## 三类数据

| 类别 | 展示方式 | 包含 TaskType |
|------|---------|------|
| **图文** | UI 展示（TextMeshPro / 视频 / 模型预览） | Purpose, Equipment, Principle, Test |
| **漫游** | FPS 移动 + 射线交互 | Training（漫游部分） |
| **步骤** | StepSystem（Processing→Step→Condition） | LineConnection, Training（步骤部分） |

## Tasks 合成

`Tasks` 属性将 6 个独立 Task 字段合成为 `List<TaskDataBase>`。允许少于 6 个（null 跳过），无最少限制。

## Clip 依赖

ProjectClip 之间**无依赖/解锁关系**，所有 Clip 可自由跳转。

## Step 数据

步骤系统中的每个 Step 包含两个通用字段：
- **tipsId**：Waiting 阶段 UI 提示文字 ID
- **audioId**：Waiting 阶段 GlobalAudioMgr 播放的音频 ID
- **conditionType**：8 种 Condition 之一

→ [JSON Schema](JSON-Schema.md) | [步骤系统](../Business/Step-System.md)
