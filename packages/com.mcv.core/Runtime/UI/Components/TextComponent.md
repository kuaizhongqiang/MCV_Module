# Contract: TextComponent

Role: 全工程唯一文本写入路径；持有 fontId / 字号 / 字重 / 颜色 / 对齐 / 多语言 key，按 `SystemData.textType` 裁决的形态下发到 Legacy Text 或 TMP。

结构：本文件只放 MonoBehaviour（字段 / 生命周期 / 对外 API / 装配 / 稳定链路的协程）。换向在 `TextComponentSwapper`（静态）、finished 状态机在 `TextFinishTracker`（普通类）、NBSP 排版在 `TextTypography`（静态）、对齐与字重对照表在 `TextAlignmentMap`（静态）、换向种子在 `TextControlSeed`。枚举 `TextFinishLayer` / `AssemblePhase` / `PendingKind` 按全工程约定登记在 `Models/EnumAll.cs`。

Fields:
fontId:string  [SerializeField] "ui"；稳定 id，对应 FontCatalogSO 条目（两种形态都生效，取不到则保持节点原有字体）
text:string  [SerializeField,TextArea] 字面量文案；无 languageKey 时显示它，同时是**中文录入面**（同步工具把它写进 SO 的 clips[0]）
languageKey:string  [SerializeField] 多语言 key（= LanguageClip.id）；有值时优先于字面量
fontSize:int  [SerializeField] 16
fontStyle:FontStyle  [SerializeField] Normal
color:Color  [SerializeField] black
alignment:OverrideAlignment  [SerializeField] Auto = 不改节点原有对齐
cjkTypography:bool  [SerializeField] false；逐节点开中文排版（NBSP 缩进 + 标点避头）
legacyText / tmpText  运行时：当前生效的控件；至多一个非空（TMP 形态下旧控件已被卸载）
assemblePhase:AssemblePhase  装配阶段（就绪门与补装的唯一判据，取代原来 ready / swapping / swapPending 的三布尔组合判定）
swapping / swapPending / swapRetried  换向协程在跑 / 待换形态 / 重试已用过（最多一次）
readySubscribed:bool  是否已订阅 GlobalDataMgr.Ready（退订在 OnDestroy）
pendingKind:PendingKind + pendingValue + pendingClip  装配期间缓冲的写入（**三态**：字面量 / key / 条目）
externallyDriven:bool  外部显式写过文案 ⇒ 不再用 languageKey / 字面量覆盖它
rawText:string  当前原文（不含排版标记）
tracker:TextFinishTracker  finished 状态机（已达层 / 帧预算 / 已注册回调）
avoiding:bool  避头协程在跑（防重入）
settleGeneration:int  稳定协程的代次：失活会打断协程，重入时靠它让旧协程退出

Methods:
Awake()  就绪门：`GlobalDataMgr.IsReady` 为真才 Assemble()，否则只订阅 Ready（**绝不用兜底值定型形态**）
OnEnable()  未激活层级起不了协程：补跑挂起的换向、挂起的 ③避头 与 ④布局 / 稳定协程
Reset()  编辑期补挂：两种文本控件都没有时给一个 TMP（编辑期默认 TMP）
OnDestroy()  退订就绪事件 + 代次自增（让在跑的稳定协程退出）+ 清空回调
OnDataReady()  就绪回调：退订 + 补装（幂等）
WantsTmp()  static；`GlobalDataMgr.GetTextType() == TextType.TMP`（形态的唯一裁决者）
Assemble()  幂等入口：置 Assembling 后按目标形态分派
AssembleLegacy()  已是 Legacy 直接用；是 TMP 走 `TextComponentSwapper.SwapToLegacy`（**不认领**节点既有控件）；都没有则补一个
AssembleTmp()  已有 TMP 直接用；只有 Legacy 时换向（Dropdown 文本节点豁免）；都没有则直接挂 TMP
CompleteSwap()  internal；换向器收尾入口 → FinishAssemble
EnterPhase(phase)  internal；换向器把失败态钉住
FailedImmediately()  没有换向过程就失败（AddComponent 被拒）：下帧按同一方向重试一次，再失败即 Failed
FinishAssemble()  装配收尾：置 Ready → ApplyStyle → ApplyPendingOrConfigured
ApplyPendingOrConfigured()  消费缓冲写入（三态分派）；否则按配置取文案，外部驱动过则补写它那个字面量
IsDropdownTextNode(node)  static；向上比对 Dropdown 的 captionText / itemText（两个方向都豁免）
SetText(value)  写运行期字面量；装配未完成则缓冲
SetTextKey(key)  写静态文案 key；装配未完成则缓冲
SetText(clip)  直接给一条 LanguageClip（不查 key 表，等价于 languageKey = clip.id）；clip 为 null 则载入字面量
Refresh()  按 key / 字面量重取；装配未完成或外部驱动过时直接返回
OnFinished(callback, layer = Layout)  注册"稳定后回调"（转发给 tracker；已达成则同帧回调、重复注册不去重）
BeginFinishPipeline()  ② 赋值 / 样式变更后启动稳定链路：tracker.Begin() → 需要避头就起协程，否则 SettleTypography()
SettleTypography()  ③ 收敛：标记 Typography → 请求 ④ → 起稳定协程
RequestFinishLayout()  ④ 向所属面板请求 `PanelBase.RequestLayoutRebuild`（**不直连** UILayoutRebuilder）；无面板祖先则 SkipLayout；未激活则留待 OnEnable
StartSettleRoutine()  起稳定协程（代次自增，旧协程随即退出）
SettleRoutine(generation)  IEnumerator；逐帧推进（**不用 Update**），到 `FinishFrameLimit` 即告警放行，全部达成即收尾
InvokeSafely(callback)  回调异常隔离
NeedsTypographyAvoidance()  `cjkTypography && legacyText != null`（避头仅 Legacy）
AvoidLeadingPunctuation()  IEnumerator；标点避头（Legacy 专用）：每轮等一帧、只挪一个行首标点，收敛后 SettleTypography()
SetTextOn(Text|TextMeshProUGUI, value)  static；有组件走组件，没有则直写
SetColorOn(Text, color)  static；有组件走 ColorValue（由 ApplyStyle 下发到当前控件），没有则直写
ReadRaw(Text|TextMeshProUGUI)  static；读原文（有组件时返回 RawText）；null / 已销毁引用返回空串
NodeOf(Text|TextMeshProUGUI, comp)  static；取文本节点（组件优先 —— 换向后旧字段是"假 null"）
Remove(s) / RemoveNewlines(s)  static；转发到 TextTypography
ApplyConfiguredText()  有 key 查 LanguageData 取当前语言（回退链唯一实现在 GlobalDataMgr.PickClipText）；无 key 用字面量
ApplyRawText(value)  记 rawText → 按需排版（TextTypography.BuildDisplay）→ 写控件 → BeginFinishPipeline()
WriteToTarget(display)  写进 legacyText 或 tmpText
ApplyStyle()  下发字体 → 字号 → 颜色 → 字重 → 对齐（字体取不到**不赋值**，故不会让文本消失）

Notes:
- 继承 MonoBehaviour 而非 ComponentBase：基类链上的 UIBase 带 [RequireComponent(typeof(CanvasGroup))]，会给几十个纯文本节点各强加一个 CanvasGroup，并被登记进 PanelBase 的组件表（语义不对）。
- **就绪门（B24）**：`GlobalDataMgr.DelayInit` 异步读 JSON 而 `Awake` 同步执行 ⇒ 早起的组件读到的 textType 还是字段初始值。用它定型等于把组件永久钉死成兜底形态，故只订阅 `GlobalDataMgr.Ready` 补装；组件自己不轮询，也不能只靠 Setup 的等待链。
- **`textType` 是形态的唯一裁决者（B1）**：不符即**强制换向**，双向对称；"节点既有优先"的认领 ❌ 不采用（会让 textType 单向失效）。换向只在装配阶段执行一次。
- 换向顺序与三级降级的完整缘由见 `TextComponentSwapper.md`；种子的"必须在卸载前抄出"见 `TextControlSeed.md`。
- **调用方契约：必须引用本组件，不能只持有 `Text`。** 换向卸载旧控件后，持有 `Text` 的调用点全变"假 null"，静态入口第一道闸 `if (target == null) return;` 会让它**静默 no-op**；故在 `Awake` / 构造时解析并缓存组件，写走 `SetText`、读走 `RawText`、颜色走 `ColorValue`。
- **调用方"不需要自己等初始化"**：装配未完成时写入先落进 pending，`FinishAssemble` 统一应用。
- **`ready`（写入安全）与 `finished`（显示稳定）不可互换**：涉及测量的动作必须在 `OnFinished` 之后；三层语义与帧上限见 `TextFinishTracker.md` 与 `EnumAll.md`。
- **`finished` 计时**：从本次内容赋值那一帧起算；样式 setter（FontSize / FontStyle / Color / Alignment）与 `Refresh()` 同样复位；内容变化时已注册回调**保留**（等这一轮重新达成）；**失活停表**（协程起不来 = 不吃帧预算）。
- **帧上限 10（K11 / K26）**：避头收敛性未证明 ⇒ 到限即按已稳定放行到 Layout + 告警，否则所有"等文本稳定"的调用方都会被一条不收敛的文本永久卡住。
- **回调三原则**：已达成再注册 ⇒ 同帧立即回调；重复注册不去重；异常逐个隔离。
- **未激活层级的两种子情形**：此后被激活 ⇒ finished 延后到 OnEnable 之后继续；始终不激活 ⇒ 永远到不了 true ⇒ 消费方必须容忍，面板侧由 `WaitAllTextFinished` 的 `timeoutFrames` 兜底（**该兜底只在面板整体激活时有效**）。
- **Dropdown 例外**靠 `IsDropdownTextNode` 自动识别（不加序列化字段、不改 prefab），命中只打 Verbose。
- **字体下发的降级路径**：取不到字体**绝不赋值**（拿 null 覆盖会让文本消失）。⚠️ 字体下发的硬前提是字体 AB 已构建（`Get*ByFontId` 只查已加载缓存、不触发加载）⇒ 没跑过字体 AB 时恒为 null，表现为"字体配置没生效"（B21）。
- **TMP 的断行与避头按已知取舍接受**：NBSP 缩进两形态都适用；标点避头依赖 TextGenerator，是 Legacy 专用。
- **别把 TMP 挂到子节点来回避卸载**：那会让同节点的 `ContentSizeFitter` 失去可测量的 Graphic（11 个节点带它、4 个带 `LayoutElement`，会塌）。
