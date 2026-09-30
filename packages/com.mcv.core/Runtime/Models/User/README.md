# Models/User —— 用户身份与实训成绩档案

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`../README.md`（数据层只读约定）；`Docs/design_ai/Score.md`（计分/落盘/上传方案）

> 路径：`Assets/Scripts/Models/User/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Models.User`

身份来源为平台注入 / 本地登录面板；成绩按学号落盘为独立 JSON。

## 选文件

- `UserData.cs` — 登录态身份：派生成绩文件名/显示名，提供身份快照
- `ScoreCalculator.cs` — 成绩核算规则（纯静态计算）：满分拆分、单价、给分与汇总

## 跨文件约定

- **`ScoreCalculator` 是计分唯一口径**：调用方不要自己算分，业务只上报「哪个计分单元完成了」（`GlobalDataMgr.ReportScoredUnit` → `ScoreCalculator.Apply`，唯一核算入口）。单价与总分规则见同级 `ScoreCalculator.md` 与 `design_ai/Score.md` §2 —— 改规则改那边。
- **`Apply` 的 `expectedUnitCount` 传 `<=0` 时不触发满分短路**（只用于中途即时重算）；**最终结算（`GlobalDataMgr.PreviewScore`）必须传实算单元数**，否则永远拿不到 100。
- **文件名与合并**：档案名取 `UserData.ScoreFileName`（有学号取学号，否则 `Anonymous`），同一学生重复实训**覆盖更新同一份**，不新建第二份；项目分/步骤分按最新一轮覆盖（不取最高分），完成标志只增不减，`attempts` 累加。
- **`UserData.json` 是只读模板**：成绩**不写回** `Data/UserData.json`，单独落 `StreamingAssets/Score/{学号|Anonymous}.json`（PC 打包后可写；WebGL 不可写，见 `Score.md §5`）。
- **成绩文件只存 `id` + 名称**，不要内嵌 `ProjectClip`（否则题库、步骤、AB 包配置会被写进成绩）。
- **`indentyNum` 是历史拼写**（应为 `indentNum`）：已作为 JSON 契约保留，改名要同步 `UserData.json` 与平台对接字段。

## 与其他目录的关系

- `Managers/GlobalDataMgr`：`VerifyLogin` / `SetUserData`（两处重载）/ `UserData` 属性、`UserData.json` 加载，以及全部成绩接口（**不另建管理器**）。
- `Controllers/LoginController`、`UI/Panels/LoginPanel`：登录交互。当前**只开放游客（`UserType.Unknow`）登录**，面板不采集学号/班级 → 档案固定落 `Anonymous.json`。
- `Models/Project/`：`ClipScore.id` ↔ `ProjectClip.id`，`TaskScore.id` ↔ `TaskDataBase.id`，`TaskScore.taskType` ↔ `TaskType`。
