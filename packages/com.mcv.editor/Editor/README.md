# Editor —— 编辑器工具（菜单两个根：`MCV Editor/` 与 `MCV Build/`）

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 结构改造（拆 `Common` / `Tools` / `BuildTools`；菜单由 `MCV/` 拆为 `MCV Editor/` 与 `MCV Build/`；三层各加 Editor-only asmdef，编译已验证）
依赖：AGENT.md、Docs/manuals_ai/Development.md §6

> 路径：`Assets/Editor/` ｜ 程序集：`MCV.Editor.Common` / `MCV.Editor.Tools` / `MCV.Editor.Build`（三者均 Editor-only）｜ 命名空间：`MCV_Module.EditorTools.Common`（仅 Common 有）

编辑器工具按"**谁在什么时候跑**"分三层：出包流程进 `BuildTools/`，编辑期生产工具与 Inspector 扩展进 `Tools/`，两者共用的内核进 `Common/`。

## 选文件

- `Common/` —— 共享内核（`EditorPaths` / `EditorAssetUtil` / `PackageConfigWriter` / `PackageDatabaseSync` / `BundleBuilder` / `GlobalBundleRunner`）；`Tools/` 与 `BuildTools/` 都依赖它，**禁止再抄一份**
- `Tools/` —— 编辑期生产工具与 Inspector 扩展（菜单根 `MCV Editor/`，共 8 个 `.cs`）：生产工具 `PanelGeneratorWindow` / `ControllerPlaceholderTools` / `DataSOExporter` / `TextI18nDataTools` / `TextMigrationTools` / `TextOverrideTools`；Inspector 扩展 `StepHandlerEditor` / `TextComponentEditor`。2026-09-30 元件业务清理已删 `LineEditTools` / `ElementLineObjEditor` / `InspectionElementObjEditor` / `InspectionMultimeterKnobObjEditor`
- `BuildTools/` —— 出包流水线（菜单根 `MCV Build/`）：`ContentBundleTools` / `CameraBgBundleTools` / `RoomOneBundleTools` / `SceneAddressableTools` + `ContentProviders/`（`InfoSpriteProvider` / `ModelPrefabProvider`）+ `GlobalProviders/`（`CameraBgGlobalProvider` / `RoomOneGlobalProvider` / `UIPrefabGlobalProvider` / `FontGlobalProvider`）

## 跨文件约定

- **分派规则**：跑完会改构建产物、或属于出包流程 → `MCV Build/`；其余编辑期工具与 Inspector 扩展 → `MCV Editor/`。
- **`MCV/` 前缀仍被 9 处 `[CreateAssetMenu]` 使用**（右键创建数据资产：`MCV/Data/*`、`MCV/Package/*`、`MCV/Scene AA Config`）——那是 Project 窗口的 Create 子菜单，**与菜单栏不是一回事，不要一起改**。
- `MCV Editor/危险/创建缺失的数据 SO` 带确认弹窗：它创建空 SO，而构建前钩子会用空 SO 覆盖 `StreamingAssets/Data/*.json`（AGENT.md 红线）。
- `ElementLineObjEditor.cs` 内含 `[InitializeOnLoad] ElementLineSceneUpdater`，每帧给全场景加 `MeshCollider`（未走 Undo）——改这个文件格外小心（AGENT.md 既有雷）。
- 新增目录 / 文件后 `.meta` 由 Unity 生成，**不要手造或手改 `.meta`**。
- **别用 `Build` 当目录名**：`ignore.conf` 第 1–4 行的 `Build` / `build` / `Builds` / `builds` **没有前导斜杠、任意深度匹配** → 任何名叫 `Build/` 的目录会被 Plastic **静默排除**（`cm add` 只报「已排除」、`cm status` 也不显示），文件进不了库。本层因此叫 `BuildTools/`。（对照：`/Tools` 是**根锚定**，只忽略仓库根的 `Tools/`，不会误伤本目录。）
- **程序集边界**：`Common`（`MCV.Editor.Common`）只引用 `MCV.Runtime`；`Tools`（`MCV.Editor.Tools`）引用 `MCV.Runtime` + `Unity.TextMeshPro` + `UnityEngine.UI`；`BuildTools`（`MCV.Editor.Build`）引用 `MCV.Runtime` + `MCV.Editor.Common` + `Unity.Addressables.Editor` + `Unity.Addressables` + `Unity.Newtonsoft.Json`。**Common 不得反向引用 Tools / BuildTools**（否则成环）；Common 的命名空间是 `MCV_Module.EditorTools.Common`。
- **全局包（`clipId` 留空）一律走 `Common/GlobalBundleRunner`**（B1.5）：条目 + 两条策略由 `BuildTools/GlobalProviders/` 里的 `IGlobalBundleProvider` 提供，工具文件只剩菜单入口，8 段流程不得再抄第二份。
