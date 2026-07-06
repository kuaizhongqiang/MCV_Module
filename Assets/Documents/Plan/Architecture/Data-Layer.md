# 数据层

## 概述

所有数据类继承 `DataBase`（提供 `id` 和 `displayName`），组织在 `MCV_Module.Data.*` 命名空间下。`GlobalDataMgr` 持有三大数据根对象：`SystemData`（系统配置）、`ProjectData`（实训项目列表）、`UserData`（用户信息）。

数据通过 `JsonReaderWriter`（Newtonsoft.Json）读写 `StreamingAssets/Data/` 下的 JSON 文件。**读为主，写低优**——JSON 主要用于读取配置，写入内容极少且不严格。

## 继承链

```
DataBase
  ├── SystemData          # 系统配置（项目信息、版权、渲染质量）
  ├── ProjectData         # 实训项目列表 + 当前 Clip 引用
  ├── UserData            # 用户凭证与成绩（低优）
  └── TaskDataBase
       └── TaskData<T>    # 自引用泛型基类
            ├── TaskPurposeData       → 图文
            ├── TaskEquipmentData     → 图文
            ├── TaskPrincipleData     → 图文
            ├── TaskLineConnectionData → 步骤
            ├── TaskTrainingData      → 漫游+步骤
            └── TaskTestData          → 图文
```

中间基类策略：`TaskDataBase` 提供 Task 通用字段，`TaskData<T>` 提供自引用泛型。SystemData / ProjectData / UserData 直接继承 DataBase，不需要额外中间层。

## TaskData 三类数据

| 类别 | TaskType | 驱动系统 |
|------|----------|---------|
| **图文** | Purpose / Equipment / Principle / Test | UI 展示（MCV） |
| **漫游** | Training（漫游部分） | FPS 漫游（MCV） |
| **步骤** | LineConnection / Training（步骤部分） | StepSystem |

TaskData 自由组合映射，一个 ProjectClip 可配任意数量和类型的 Task，无最少限制。

## JSON 读写

### 文件清单

| 文件 | 对应类 | 读写策略 |
|------|--------|---------|
| `SystemData.json` | `SystemData` | 启动时读一次，不写回 |
| `ProjectData.json` | `ProjectData` | 启动时读取，运行时查询 |
| `UserData.json` | `UserData` | 低优，暂不议 |

### 容错策略

| 场景 | 处理 |
|------|------|
| 文件不存在 | 使用代码默认值，可选写回默认文件 |
| 字段缺失 | 使用字段类型默认值（int→0，string→""，bool→false） |
| JSON 格式错误 | 日志报错，使用默认值，不崩溃 |
| 类型不匹配 | Newtonsoft.Json 自动处理，转换失败抛异常（已 try-catch） |

### 读写时机

```
启动 → GlobalDataMgr.DelayInit()
  → Read<SystemData>("SystemData")
  → Read<ProjectData>("ProjectData")
  → isInit = true

运行时 → Controller 通过 GlobalDataMgr.Instance.ProjectData 查询
退出 → 如需保存进度 → Write<UserData>("UserData")（低优）
```

## Step 数据模型

步骤系统需要独立的 Model 层（StepAll / ProcessingData / StepData），参考 Tuanjie 项目 `Data/` 路径设计：

```
StepAll
  └── clipSteps: List<ClipStep>
       └── processingData: List<ProcessingData>
            └── stepData: List<StepData>
                 ├── id, displayName, Type, Status, Index
                 ├── tipsId（Waiting 时 UI 提示文字 ID）
                 └── audioId（Waiting 时音频 ID）
```

运行时由 `StepDirector.InitDataFromObj()` 从场景层级写入 `StepAll`，不依赖外部 JSON。`StepAll` 仅用于按 ID 查询（`SetStep(string stepId)`）。

## 序列化规范

```csharp
[Serializable]
public class TaskPurposeData : TaskData<TaskPurposeData>
{
    [SerializeField] private string description;
    [SerializeField] private string imagePath;
    
    [NonSerialized] private bool isDirty;  // 运行时标记，不序列化

    public string Description => description;
    public string ImagePath => imagePath;

    public TaskPurposeData(string id)
    {
        this.id = id;
        displayName = "任务目的";
    }
}
```

**规则**：
- 所有数据类标记 `[Serializable]`
- 存储字段用 `[SerializeField] private`，外部通过公共 `{ get; }` 属性访问
- 运行时仅字段标记 `[NonSerialized]`
- 构造函数负责 `id` 与默认 `displayName`

→ [编码规范](../Guides/Coding-Conventions.md) | [JSON Schema](../Data/JSON-Schema.md) | [ProjectClip 规范](../Data/ProjectClip-Spec.md)
