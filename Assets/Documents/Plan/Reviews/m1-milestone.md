# M1 Milestone Review

**Date**: 2026-07-06 | **Version**: 2 | **Reviewer**: PM CodeBuddy

**Scope**: M1 v2 审查 — 5 个 PR (#29-#32, #37) 已全部合并到 main。检查 61 个 .cs 文件的编译正确性和上次 Review 修复情况。

---

## 发现汇总

| Severity | Count |
|----------|:-----:|
| Critical | 0 |
| Major    | 3 |
| Minor    | 3 |
| Suggestion | 1 |
| **Total** | **7** |

---

## v1 → v2 修复确认

| # | v1 发现 | v2 状态 |
|:---|:---|:---:|
| 1 | `TaskDefrojectData` 拼写错误 | ✅ 已移除 |
| 2 | 4 个 PR 未关联 Milestone | ⚠️ 需 GitHub 侧手动操作 |
| 3 | `ProjectData.cs` 缺 `[SerializeField]` | ⚠️ 字段已加但用了 `public` |
| 4 | `ControllerBase` 无 `OnViewBound()` | ✅ 已添加 `abstract void OnViewBound()` |
| 5 | `Setup.cs` 未注册 `GlobalStepSystemMgr` | ✅ 已添加（第 75-82 行） |
| 6 | Panel 命名空间不统一 | ✅ 全部使用 `MCV_Module.UI.Panels` |
| 7 | `JsonReaderWriter` 缺命名空间 | ✅ 已添加 `MCV_Module.Data.Json` |
| 8 | PR #32 自测未勾选 | ⚠️ 仍建议 Unity Editor 验证 |
| 9 | Bug #9 影响验证 | ⚠️ `IsGlobalMgrInit<T>` 仍硬编码 true |
| 10 | PR 粒度建议 | ℹ️ 非代码问题，无需处理 |

---

## Major 发现

### 1. Controller 命名空间与 Milestones.md 声明不一致

- **Severity**: Major
- **Location**: 28 个 Controller 文件 + `Assets/Scripts/Controller/ControllerBase.cs`
- **Problem**: 实际 Controller 命名空间为 `MCV_Module.Controller`（单数），文件夹名也为 `Controller/`（单数）。但 Milestones.md M1c 节规定命名空间为 `MCV_Module.Controllers`（复数）。PR #31 描述中使用的是 `MCV_Module.Controllers`。
- **Suggestion**: 统一为一种方案。建议保持当前 `MCV_Module.Controller`（与文件夹 `Controller/` 一致），同时更新 Milestones.md 文档。
- **相关 Issue**: 需新建

### 2. TaskData 字段使用 `public` 违反编码规范

- **Severity**: Major
- **Location**: `Assets/Scripts/Data/Project/ProjectData.cs` — 6 个 TaskData 子类
- **Problem**: TaskData 子类中的新增字段声明为 `public`（如 `public string showContent`、`public SpeakingData speakingData`）。编码规范明确规定 `[SerializeField] private` + 公共 `{ get; }` 属性的模式。当前写法不影响编译但不符合项目规范。
- **Suggestion**: 将字段改为 `[SerializeField] private` 并添加 `public { get; private set; }` 属性。
- **相关 Issue**: 需新建

### 3. `ControllerBase` 路径不符合架构约定

- **Severity**: Major
- **Location**: `Assets/Scripts/Controller/ControllerBase.cs`
- **Problem**: `ControllerBase` 放在 `Controller/` 目录下而非 `Architecture/` 目录下。从架构角度看，ControllerBase 和 UIBase、PanelBase 等同属基础设施层。Overview.md 中 MCV 架构图提示 `ControllerBase` 是架构组件，但实际放在 `Controller/`（业务层目录）中。
- **Suggestion**: 移动 `ControllerBase.cs` 到 `Assets/Scripts/Architecture/ControllerBase.cs`，同步更新所有 28 个 Controller 的 using 引用。
- **相关 Issue**: 需新建

---

## Minor 发现

### 4. `GlobalStepSystemMgr` 目录位置

- **Severity**: Minor
- **Location**: `Assets/Scripts/StepSystem/GlobalStepSystemMgr.cs`
- **Problem**: `GlobalStepSystemMgr` 放在独立的 `StepSystem/` 目录下，而非与其他 8 个 GlobalManager 一起放在 `GlobalManager/` 目录。这可能不影响功能，但破坏了一致性。
- **Suggestion**: 移动到 `Assets/Scripts/GlobalManager/GlobalStepSystemMgr.cs`，或保持现状并说明理由。

### 5. 部分占位符文件无 `.meta` 文件验证

- **Severity**: Minor
- **Location**: 61 个新建/修改的 .cs 文件
- **Problem**: 未能在代码层面验证所有新文件是否都生成了 .meta 文件。Unity 缺少 .meta 文件会导致 GUID 冲突和资源引用断裂。PR #32 (M1d) 验证清单中要求"所有 .cs 文件有对应 .meta 文件"但标注"自测未通过"。
- **Suggestion**: 在 Unity Editor 中打开项目并验证所有新文件有 .meta 文件。

### 6. `Setup.cs` 初始化逻辑仍有硬编码

- **Severity**: Minor
- **Location**: `Assets/Scripts/Setup.cs`
- **Problem**: `IsGlobalMgrInit<T>()` 方法始终返回 `true`（硬编码），不检查真实初始化状态。GlobalManager.md 中描述的"重试 + 超时 + Quit"策略未实现。
- **Suggestion**: M2 中实现真实的 `IsGlobalMgrInit<T>()` 检查逻辑。

---

## Suggestion

### 7. 建议增加 M1 完成度的 GitHub Check

- **Severity**: Suggestion
- **Location**: CI 工作流
- **Problem**: 当前 `.github/workflows/build.yml` 使用 Unity batchmode 打包，但该流程不验证"文件数量是否达到 61 个"。可考虑在 CI 中添加文件数量检查脚本。
- **Suggestion**: 添加 `gh pr checks` 或文件计数脚本作为 PR Check。

---

## PR 状态总览

| PR | 标题 | 状态 | 新建文件 | 修改文件 |
|:---|------|:---:|:------:|:------:|
| #29 | M1a Data 层补齐 | ✅ MERGED | 4 | 1 |
| #30 | M1b Panel 层占位符 | ✅ MERGED | 21 | 7 |
| #31 | M1c Controller 层占位符 | ✅ MERGED | 28 | 0 |
| #32 | M1d 编译验证清单 | ✅ MERGED | 0 | 1 (doc) |
| #37 | fix(M1 Review) | ✅ MERGED | 0 | 5 |
| **合计** | | | **53** | **14** |

## Issue 状态

| # | 标题 | Severity | M1 完成前需解决? |
|:---|------|:---|:---:|
| #33 | TaskDefrojectData 拼写错误 | Critical | ✅ 已 resolved (通过 #37) |
| #34 | ControllerBase.OnViewBound 未定义 | Critical | ✅ 已 resolved (通过 #37) |
| #35 | Setup.cs 未预留 StepSystemMgr 槽位 | Major | ✅ 已 resolved (通过 #37) |
| #36 | Panel 命名空间需统一 | Major | ✅ 已 resolved (通过 #30) |
