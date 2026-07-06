# ProjectClip 数据规范

## 概述

`ProjectClip` 是核心业务实体，继承 `DataBase`。每个 Clip 包含 6 种 `TaskDataBase` 子类，对应实验的 6 个教学环节：

| TaskType | Data 类 | 说明 |
|----------|---------|------|
| `Purpose` | `TaskPurposeData` | 任务目的 |
| `Equipment` | `TaskEquipmentData` | 实验仪器 |
| `Principle` | `TaskPrincipleData` | 实验原理 |
| `LineConnection` | `TaskLineConnectionData` | 电路连接 |
| `Training` | `TaskTrainingData` | 仿真实验 |
| `Test` | `TaskTestData` | 小测验 |

`Tasks` 属性将 6 个独立字段合成为 `List<TaskDataBase>`。

## 待完善

- 各 TaskData 的扩展字段定义
- Clip 之间的依赖与解锁关系
