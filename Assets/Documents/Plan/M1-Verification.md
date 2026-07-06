# M1 编译验证清单

> 验证日期：2026-07-06
> 关联 PR：M1a (#29) / M1b (#30) / M1c (#31)

## 验收标准

### 1. 文件完整性

| 类别 | 要求 | 实际 | 状态 |
|------|:----:|:----:|:----:|
| Data 层文件 | 10 | — | ⏳ |
| Panel 层文件 | 28 | 28 | ✅ |
| Controller 层文件 | 28 | 28 | ✅ |

**Data 层明细（10 个）：**

| # | 文件 | 操作 | 状态 |
|---|------|:----:|:----:|
| 1 | `Data/DataBase.cs` | 已存在 | ✅ |
| 2 | `Data/Project/ProjectData.cs` | 修改 | ✅ (PR #29) |
| 3 | `Data/Project/LineData.cs` | 新建 | ✅ (PR #29) |
| 4 | `Data/Project/QuestionData.cs` | 新建 | ✅ (PR #29) |
| 5 | `Data/Project/StepData.cs` | 新建 | ✅ (PR #29) |
| 6 | `Data/Project/TaskDataConverter.cs` | 新建 | ✅ (PR #29) |
| 7 | `Data/System/SystemData.cs` | 已存在 | ✅ |
| 8 | `Data/User/UserData.cs` | 已存在 | ✅ |
| 9 | `Data/Addressable/*.cs` | 已存在 | ✅ |
| 10 | `Data/Json/JsonReaderWriter.cs` | 已存在 | ✅ |

**Panel 层明细（28 个）：**

| # | 文件 | 类名 | TODO | 状态 |
|---|------|:----:|:----:|:----:|
| 1 | `MenuPanel.cs` | MenuPanel | M2 | ✅ |
| 2 | `TitlePanel.cs` | TitlePanel | M2 | ✅ |
| 3 | `TaskPanel.cs` | TaskPanel | M2 | ✅ |
| 4 | `DialogPanel.cs` | DialogPanel | M2 | ✅ |
| 5 | `TipsPanel.cs` | TipsPanel | M2 | ✅ |
| 6 | `FunctionPanel.cs` | FunctionPanel | M2 | ✅ |
| 7 | `CopyrightPanel.cs` | CopyrightPanel | M2 | ✅ |
| 8 | `StartPanel.cs` | StartPanel | M2 | ✅ |
| 9 | `LoadingPanel.cs` | LoadingPanel | M2 | ✅ |
| 10 | `LoginPanel.cs` | LoginPanel | M5 | ✅ |
| 11 | `LeftBottomPanel.cs` | LeftBottomPanel | M2 | ✅ |
| 12 | `RightUpPanel.cs` | RightUpPanel | M2 | ✅ |
| 13 | `RoamingBottomPanel.cs` | RoamingBottomPanel | M2 | ✅ |
| 14 | `ControlInfoPanel.cs` | ControlInfoPanel | M2 | ✅ |
| 15 | `AiDialogPanel.cs` | AiDialogPanel | M4 | ✅ |
| 16 | `SleepTipsPanel.cs` | SleepTipsPanel | M4 | ✅ |
| 17 | `ResultSummitPanel.cs` | ResultSummitPanel | M4 | ✅ |
| 18 | `RenderQualityPanel.cs` | RenderQualityPanel | M5 | ✅ |
| 19 | `StepPanel.cs` | StepPanel | M3 | ✅ |
| 20 | `StepProcessingPanel.cs` | StepProcessingPanel | M3 | ✅ |
| 21 | `StepQuestionPanel.cs` | StepQuestionPanel | M3 | ✅ |
| 22 | `StepToolPanel.cs` | StepToolPanel | M3 | ✅ |
| 23 | `Task_PurposePanel.cs` | TaskPurposePanel | M2 | ✅ |
| 24 | `Task_EquipmentPanel.cs` | TaskEquipmentPanel | M2 | ✅ |
| 25 | `Task_PrinciplePanel.cs` | TaskPrinciplePanel | M2 | ✅ |
| 26 | `Task_LineConnectionPanel.cs` | TaskLineConnectionPanel | M2 | ✅ |
| 27 | `Task_TrainingPanel.cs` | TaskTrainingPanel | M2 | ✅ |
| 28 | `Task_TestPanel.cs` | TaskTestPanel | M2 | ✅ |

**Controller 层明细（28 个）：**

| # | 文件 | 绑定 Panel | TODO | 状态 |
|---|------|:---------:|:----:|:----:|
| 1 | `MenuController.cs` | MenuPanel | M2 | ✅ |
| 2 | `TitleController.cs` | TitlePanel | M2 | ✅ |
| 3 | `TaskListController.cs` | TaskPanel | M2 | ✅ |
| 4 | `DialogController.cs` | DialogPanel | M2 | ✅ |
| 5 | `TipsController.cs` | TipsPanel | M2 | ✅ |
| 6 | `FunctionController.cs` | FunctionPanel | M2 | ✅ |
| 7 | `CopyrightController.cs` | CopyrightPanel | M2 | ✅ |
| 8 | `StartController.cs` | StartPanel | M2 | ✅ |
| 9 | `LoadingController.cs` | LoadingPanel | M2 | ✅ |
| 10 | `LoginController.cs` | LoginPanel | M5 | ✅ |
| 11 | `LeftBottomController.cs` | LeftBottomPanel | M2 | ✅ |
| 12 | `RightUpController.cs` | RightUpPanel | M2 | ✅ |
| 13 | `RoamingBottomController.cs` | RoamingBottomPanel | M2 | ✅ |
| 14 | `ControlInfoController.cs` | ControlInfoPanel | M2 | ✅ |
| 15 | `AiDialogController.cs` | AiDialogPanel | M4 | ✅ |
| 16 | `SleepTipsController.cs` | SleepTipsPanel | M4 | ✅ |
| 17 | `ResultSummitController.cs` | ResultSummitPanel | M4 | ✅ |
| 18 | `RenderQualityController.cs` | RenderQualityPanel | M5 | ✅ |
| 19 | `StepController.cs` | StepPanel | M3 | ✅ |
| 20 | `StepProcessingController.cs` | StepProcessingPanel | M3 | ✅ |
| 21 | `StepQuestionController.cs` | StepQuestionPanel | M3 | ✅ |
| 22 | `StepToolController.cs` | StepToolPanel | M3 | ✅ |
| 23 | `Task_PurposeController.cs` | TaskPurposePanel | M2 | ✅ |
| 24 | `Task_EquipmentController.cs` | TaskEquipmentPanel | M2 | ✅ |
| 25 | `Task_PrincipleController.cs` | TaskPrinciplePanel | M2 | ✅ |
| 26 | `Task_LineConnectionController.cs` | TaskLineConnectionPanel | M2 | ✅ |
| 27 | `Task_TrainingController.cs` | TaskTrainingPanel | M2 | ✅ |
| 28 | `Task_TestController.cs` | TaskTestPanel | M2 | ✅ |

### 2. 继承正确性

- [ ] 所有 Panel 继承 `PanelBase`
- [ ] 所有 Controller 继承 `ControllerBase<T>`
- [ ] Data 类使用 `[Serializable]` 标记

### 3. 命名空间

- [ ] Data: `MCV_Module.Data.*`
- [ ] Panel: `MCV_Module.UI.Panels`
- [ ] Controller: `MCV_Module.Controller`

### 4. 占位符规范

- [ ] 每个非空类包含 `// TODO: [Mx] [简述]` 注释
- [ ] TODO 注释指向正确的 Milestone

### 5. 编译

- [ ] Unity Editor 打开项目后无编译错误（红色报错）
- [ ] `Assembly-CSharp` 编译通过

### 6. 版本控制

- [ ] 所有新 `.cs` 文件有对应 `.meta` 文件
- [ ] 所有 `.meta` 文件 GUID 无冲突

---

## 验证操作步骤

1. 拉取最新代码：`git pull origin main`
2. 用 Unity 2022.3.53f1 打开项目
3. 等待编译完成
4. 检查 Console 窗口：无红色 Error
5. 检查 `Project` 窗口确认所有文件存在：
   - `Assets/Scripts/Data/Project/` — 6 个文件
   - `Assets/Scripts/UI/Panels/` — 28 个文件
   - `Assets/Scripts/Controllers/` — 28 个文件
6. 若有编译错误，回退对应 PR 修复后重试

---

## 风险备注

- `.meta` 文件 GUID 为手动生成，若 Unity 报 GUID 冲突，删除 `.meta` 后让 Unity 重新生成即可
- `TaskDefrojectData` 类名疑为拼写遗留问题，不影响编译，可后续 M2 时确认是否清理
