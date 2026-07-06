# 编码规范

## 命名规范

### 命名空间

跟随文件夹层级，前缀 `MCV_Module`。

```
Assets/Scripts/Data/Project/   →  MCV_Module.Data.Project
Assets/Scripts/GlobalManager/  →  MCV_Module.GlobalManager
Assets/Scripts/UI/             →  MCV_Module.UI
```

### 类与接口

| 类型 | 规范 | 示例 |
|------|------|------|
| 类 | PascalCase | `GlobalDataMgr`, `TaskPurposeData` |
| 接口 | `I` 前缀 + PascalCase | `IController`, `IObj` |
| 抽象类 | `Base` 后缀 或 `DataBase` | `ControllerBase`, `DataBase` |
| 泛型类 | 单字母类型参数 `<T>` | `SingletonGlobalMgr<T>`, `ControllerBase<T>` |
| 枚举 | PascalCase | `TaskType`, `PackageType` |

### 字段与属性

| 类型 | 规范 | 示例 |
|------|------|------|
| 私有字段 | camelCase | `systemData`, `isInit` |
| `[SerializeField]` 私有字段 | camelCase | `taskPurposeData` |
| 公共属性 | PascalCase，`get`/`set` | `IsInit`, `SystemData` |
| 静态字段 | `s_` 前缀 + camelCase | `s_Instance`, `s_AppQuitting` |
| 常量 | PascalCase 或 `c_` 前缀 | — |

**原则**：能私有不公有，能 `{ get; }` 不 `{ get; set; }`。

### 方法

| 类型 | 规范 |
|------|------|
| 公共方法 | PascalCase |
| 私有方法 | PascalCase |
| 回调/协程 | PascalCase，语义明确 | `DelayInit()`, `OnApplicationQuit()` |

---

## 代码组织

### 文件结构

每个 `.cs` 文件只包含一个主类。关联的小型枚举/辅助类可放同一文件（如 `ProjectData.cs` 包含所有 TaskData 子类与 `TaskType` 枚举）。

```
Assets/Scripts/
├── Singleton/           # 基础设施（无业务逻辑）
├── GlobalManager/       # 全局管理器，一文件一类
├── Controller/          # Controller 接口与基类
├── Data/
│   ├── DataBase.cs      # 数据基类
│   ├── Json/            # JSON 工具
│   ├── Addressable/     # 资源管理
│   ├── Project/         # 实验数据模型
│   ├── System/          # 系统数据模型
│   └── User/            # 用户数据模型
├── UI/
│   ├── UIBase.cs        # UI 基类
│   ├── CanvasBase.cs / PanelBase.cs
│   ├── Canvas/          # 具体 Canvas 实现
│   └── Panels/          # 具体 Panel 实现
└── Interactive/         # 交互系统
```

### 类内部布局

```
class Foo
{
    // 1. 静态成员
    // 2. SerializeField 字段
    // 3. 公共属性
    // 4. 私有字段
    // 5. Unity 生命周期（Awake / Start / OnEnable / OnDisable / OnDestroy）
    // 6. 公共方法
    // 7. 私有方法
    // 8. 协程
}
```

---

## 数据类规范

所有数据类继承 `DataBase`（提供 `id` 与 `displayName`），标记 `[Serializable]`。

```csharp
[Serializable]
public class TaskPurposeData : TaskData<TaskPurposeData>
{
    public override TaskType TaskType => TaskType.Purpose;

    public TaskPurposeData(string id)
    {
        this.id = id;
        displayName = "任务目的";
    }
}
```

**规则**：
- 序列化字段用 `[SerializeField] private`，外部通过公共属性访问
- 运行时仅字段（不会被序列化保存的）标记 `[NonSerialized]`
- 数据类的构造函数负责设置 `id` 与默认 `displayName`
- JSON 读写统一通过 `JsonReaderWriter.Read<T>()` / `JsonReaderWriter.Write<T>()`

---

## 全局管理器规范

继承 `SingletonGlobalMgr<T>`，通过 `Instance` 访问。

```csharp
public class GlobalDataMgr : SingletonGlobalMgr<GlobalDataMgr>
{
    protected GlobalDataMgr() { }

    protected override IEnumerator DelayInit()
    {
        SystemData = JsonReaderWriter.Read<SystemData>("SystemData", null);
        yield break;
    }
}
```

**规则**：
- 必须提供 `protected` 无参构造函数（防外部 new）
- 初始化逻辑写入 `DelayInit()` 协程，不要写在 `Awake()` 或 `Start()` 中
- `DelayInit()` 完成后 `isInit = true`，`Setup.cs` 依赖此标记推进初始化流程
- 静态辅助方法放在 `#region 静态方法` 中（参考 `GlobalDataMgr.GetTaskData()`）

---

## 版本控制（Plastic SCM / Unity Version Control）

### 术语对照

| Plastic SCM | 说明 |
|-------------|------|
| **Changeset** | 等同 Git commit：一次提交的变更集合 |
| **Branch** | 等同 Git branch：开发分支 |
| **Label** | 等同 Git tag：给 changeset 打可读标记（如 `v0.1-alpha`） |
| **Code Review** | 功能上等同 Git Pull Request：分支合并前的审查流程 |
| **Shelve** | 等同 Git stash：暂存未完成的修改 |
| **Workspace** | 等同 Git 工作区 + 本地仓库：本地开发环境 |

**没有的概念**：
- **Issue** — Plastic 无内置 Issue 跟踪。任务管理依靠本文档库 `Plan/` 中的文档追踪状态，或接入外部工具（Jira 等）
- **Milestone** — 无内置里程碑。通过 Label 标记版本节点即可

### 分支规范

```
/main              # 主分支，始终可运行
/develop           # 开发分支，日常集成
/feature/xxx       # 功能分支，从 develop 分出
/fix/xxx           # 修复分支，从 develop 或 main 分出
```

### Changeset 信息

```
类型: 概述

改动内容简述

# 示例
feat: 新增 GlobalSceneMgr Additive 加载

实现 Setup → 1+2+N 的场景加载管线
```

**类型前缀**：`feat`（新功能）、`fix`（修复）、`refactor`（重构）、`docs`（文档）、`chore`（杂项）。

### Code Review 流程

1. 功能分支开发完成，在 Plastic GUI 或 Web Dashboard 发起 Code Review
2. 指定至少一名 Reviewer
3. Review 通过后合并到 `/develop`
4. 版本发布时将 `/develop` 合并到 `/main`，打 Label

### Label 规范

格式：`v{major}.{minor}-{tag}`

```
v0.1-alpha    # 框架基本可跑
v0.2-alpha    # 漫游 + UI 展示可用
v0.3-beta     # 步骤系统可用
v1.0          # 首个实验全流程跑通
```

---

## Unity 场景与资源

- 场景文件编号前缀：`0_Setup`、`1_Controller`、`2_UI`，实验场景为 `N_` 前缀
- Prefab 按功能分目录，不堆在根目录
- ScriptableObject 配置放 `Assets/Settings/` 或功能对应目录
