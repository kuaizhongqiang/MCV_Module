# Models/System —— 软件系统数据与多语言数据模型

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：`../README.md`（数据层只读与设置类例外）

> 路径：`Assets/Scripts/Models/System/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.Models.System`

数据来自 `Assets/StreamingAssets/Data/SystemData.json` 与 `LanguageData.json`（由 `SystemDataSO` / `LanguageDataSO` 导出），由 `Managers/GlobalDataMgr` 加载，供标题栏、文本组件与设置项读取。

## 选文件

- `SystemData.cs` — 系统配置根：聚合软件信息、版权、渲染质量与多语言条目

## 跨文件约定

- **`LanguageClip.clips` 按语言枚举下标开槽**（Chinese=0、English=1…），`TextComponent` 直接用 `(int)languageType` 取项：**增删语言必须保持枚举与数组顺序一致**，否则错位。
- **多语言文案唯一真源是 `LanguageData.json`**：`TextComponent` 先用 `languageClip.id` 反查最新内容，取不到才回落组件上的快照字段。
- **新增系统级字段要同步 `SystemDataSO` 与 JSON**；加载入口 `Managers/GlobalDataMgr`。
