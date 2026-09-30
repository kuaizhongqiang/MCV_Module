# UI/Components —— 可复用原子 UI 单元

读者：AI
类型：功能文档
权威：说明
状态：2026-09-26 补 `PreviewRig`（本文件仍为定位层；各组件细节指向同级同名 `.md`）
依赖：UI/{ComponentBase,PanelBase}.cs、Managers/{GlobalDataMgr,GlobalAssetsMgr}、Models/{System,EnumAll}

> 路径：`Assets/Scripts/UI/Components/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.UI.Components`

## 选文件
- `TextComponent.cs` — 文本统一入口：Legacy/TMP + 多语言按 id 反查
- `InputFieldComponent.cs` — TMP 输入框封装：配置 + Value/HasValue/Clear
- `VideoComponent.cs` — 视频组件（当前只有配置字段）
- `PreviewRig.cs` — 模型预览装置：给面板的 RawImage **运行期自建**材质 + color/alpha 两张 RT，并挂到展示根的两台相机上（Info / Structure 面板用）

## 跨文件约定
- 文本一律用 `TextComponent`，不要在面板里直接操作 `Text` / `TextMeshProUGUI`；多语言只维护 `LanguageData.json`。
- 组件不引用 `Controllers`；面板重建即组件重建，不要存跨面板状态。
- 新增组件：继承 `ComponentBase` 实现 `ComponentType`，并同步本 README 与 `UI/README.md`。
- **`PreviewRig` 是刻意的例外**：它不继承 `ComponentBase`（不参与 `ComponentType` 配置体系），因为它靠 `OnEnable/OnDisable/OnDestroy` 管自己 new 出来的材质与 RT 生命周期；面板重建即随之重建，正好符合「不存跨面板状态」。
