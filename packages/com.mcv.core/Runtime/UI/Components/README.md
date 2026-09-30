# UI/Components —— 可复用原子 UI 单元

读者：AI
类型：功能文档
权威：说明
状态：2026-09-30 按拆分纪律重写 `TextComponent` 并补 5 个同级文件（本文件仍为定位层；各组件细节指向同级同名 `.md`）
依赖：UI/{ComponentBase,PanelBase}.cs、Managers/{GlobalDataMgr,GlobalAssetsMgr}、Models/{System,EnumAll}

> 路径：`Assets/Scripts/UI/Components/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.UI.Components`

## 选文件
- `TextComponent.cs` — 文本统一入口（MonoBehaviour）：形态裁决 + 装配 + 对外写入 API + 稳定链路的协程
- `TextComponentSwapper.cs` — 形态换向器（静态类）：Legacy↔TMP 双向换组件 + 失败三级降级
- `TextFinishTracker.cs` — `finished` 状态机（普通类）：已达层 / 帧预算 / 已注册回调，由组件协程驱动
- `TextControlSeed.cs` — 换向种子（两个 struct）：被换掉控件的节点既有设置快照与两形态换算
- `TextTypography.cs` — 中文排版纯函数（静态类）：NBSP 缩进的写入与还原
- `TextAlignmentMap.cs` — 字重 / 对齐对照表与下发（静态类）
- `InputFieldComponent.cs` — TMP 输入框封装：配置 + Value/HasValue/Clear
- `VideoComponent.cs` — 视频组件（当前只有配置字段）
- `PreviewRig.cs` — 模型预览装置：给面板的 RawImage **运行期自建**材质 + color/alpha 两张 RT，并挂到展示根的两台相机上（Info / Structure 面板用）

## 跨文件约定
- 文本一律用 `TextComponent`，不要在面板里直接操作 `Text` / `TextMeshProUGUI`；多语言只维护 `LanguageData.json`。
- 组件不引用 `Controllers`；面板重建即组件重建，不要存跨面板状态。
- 新增组件：继承 `ComponentBase` 实现 `ComponentType`，并同步本 README 与 `UI/README.md`。
- **`PreviewRig` 是刻意的例外**：它不继承 `ComponentBase`（不参与 `ComponentType` 配置体系），因为它靠 `OnEnable/OnDisable/OnDestroy` 管自己 new 出来的材质与 RT 生命周期；面板重建即随之重建，正好符合「不存跨面板状态」。
- **`TextComponent` 的拆分纪律**：`.cs` 里只留 MonoBehaviour 与「必须访问序列化字段」的那部分；纯函数（排版、对照表）、独立状态机（finished）与换向流程各自独立成文件。**组件不占用 `Update` 周期**（逐帧推进一律用协程），`#region MonoBehaviour 生命周期` 里只放 `Awake / OnEnable / Reset / OnDestroy` 这四个回调。
