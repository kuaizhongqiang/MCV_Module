# UI/Tools —— 与 MonoBehaviour 解耦的可复用 UI 逻辑（普通类 + 回调）

读者：AI
类型：功能文档
权威：说明
状态：2026-09-24 压缩为定位层（细节指向同级 `A.md`）
依赖：Managers/{GlobalDataMgr,GlobalAudioMgr,GlobalAddressableMgr}、Models/EnumAll.cs、Interfaces/IUiEffect

> 路径：`Assets/Scripts/UI/Tools/` ｜ 程序集：`MCV.Runtime` ｜ 命名空间：`MCV_Module.UI.Tools`

## 选文件
- `VideoTool.cs` — Unity VideoPlayer 的静态封装（框架播放视频的唯一入口）
- `AiBubbleStruct.cs` — AI 气泡封装：按预制体建气泡并写纯文本
- `UILayoutRebuilder.cs` — 布局重建唯一实现：等一帧 + 按深度自下而上（文本 / 子物体改动后统一走它）
- `UiAudioEffect.cs` — UI 音效基类：划过/离开/点击播 AudioEffectType
- `TipsContentUtiliy.cs` — 单条提示的进出场动画与文本/图片切换

> 2026-09-30 清理：`MenuScrollLogic` / `MenuDetailLogic`（菜单封面流与子目录逻辑）与 `ResultSummitViewData`（成绩预览视图数据）随菜单 / 成绩业务移除。

## 跨文件约定
- **改完文本 / 子物体显隐要刷布局，一律走 `UILayoutRebuilder`**（面板走 `PanelBase.RequestLayoutRebuild`，它再叠上防重入与失活跳过）：同帧直接 `LayoutRebuilder.ForceRebuildLayoutImmediate` 会量到 TMP 形态下"还没装配完的空文本"，而且必须**子先父后**（父级 LayoutGroup 的 `childControlWidth = false`，量的是子节点当前的 `sizeDelta`）。
- **普通类不持有协程**：逐帧逻辑（`MenuScrollLogic` / `MenuDetailLogic`）返回 `IEnumerator`，由宿主面板 `StartCoroutine` + `Action` 回调刷新布局。
- **第三方插件零依赖**：包内只用 Unity 原生播放器；框架**不提供播放器替换口**（原 `IVideoPlayer` 抽象已于 2026-09-30 删除），宿主若要换播放器就在自己的层里实现。
- **资源路径用常量字符串**：预制体加载失败只打警告并降级，不抛异常。
- 新增工具类：优先做普通类 + 回调，不要为了用协程而做成 MonoBehaviour。
- **坑**：`MenuScrollLogic` / `MenuDetailLogic` 是 MenuPanel 旧环形封面流的残留实现，全工程已无引用（`MenuPanel` 改走 `btnsParent` 器件按钮）—— 改 MenuPanel 前先确认是否还需要它们。
- **中文排版已并入 `UI/Components/TextComponent`**：`ChineseText`（连它的 `CoroutineRunner` / `TextUtils`）已删除；NBSP 缩进 + 标点避头改由节点上的 `cjkTypography` 开关控制（`TipsPanel` 的两个提示正文节点开着它）。写文案一律走 `TextComponent.SetTextOn`，读原文走 `TextComponent.ReadRaw`。
