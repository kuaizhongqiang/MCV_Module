# Contract: TextComponent

Role: 全工程唯一文本写入路径；持有 fontId / 字号 / 字重 / 颜色 / 对齐 / 多语言 key，按当前形态下发到 Legacy Text 或 TMP。

Fields:
fontId:string  [SerializeField] "ui"；稳定 id，对应 FontCatalogSO 条目（两种形态都生效，取不到则保持节点原有字体）
text:string  [SerializeField,TextArea] 字面量文案；无 languageKey 时显示它
languageKey:string  [SerializeField] 多语言 key（= LanguageClip.id）；有值时优先于字面量
fontSize:int  [SerializeField] 16
fontStyle:FontStyle  [SerializeField] Normal；字重（Legacy FontStyle ↔ TMP FontStyles）——实测 23 个节点是 Bold，必须有落点
color:Color  [SerializeField] black
alignment:OverrideAlignment  [SerializeField] Auto = 不改节点原有对齐；Justified 在 Legacy 下同样不改
cjkTypography:bool  [SerializeField] false；逐节点开中文排版（NBSP 缩进 + 标点避头）
legacyText:Text  运行时：当前生效的 Legacy 控件（目标形态 Legacy 时优先认领节点上已有的）；与 tmpText 至多一个非空。**TMP 形态下它被置空且那个 Legacy 组件已被卸载（先禁用再 Destroy）**，见 Notes 的"必须先卸载"
tmpText:TextMeshProUGUI  运行时：当前生效的 TMP 控件（目标形态 TMP 时才有值）
ready:bool  装配完成标志；未完成时写入进 pending —— B4 起该窗口至少跨一帧（换组件要等销毁生效），故缓冲必须真的生效
swapping:bool  换组件协程在跑；OnEnable 用它防重入（协程在跑时不再重复起）
swapPending:bool  要换形态但当时起不了协程（未激活层级），等 OnEnable 补跑
pendingValue / pendingIsKey  装配前收到的写入的价值与类型（字面量 / key）
externallyDriven:bool  外部调用过 SetText ⇒ Refresh 不再覆盖它
rawText:string  当前原文（不含排版标记）

Methods:
Awake()  Assemble —— **语言与形态都在此读一次**（两者都是设置类数据、冷启动才变，故不做运行期热切）；消费 pending / 取配置文案统一由 FinishAssemble 收尾，不再在 Awake 里各写一份
OnEnable()  未 ready 且待换标记还在且仍 WantsTmp 时补跑换组件协程：未激活层级起不了协程，换形态只能推迟到这里（swapping 防重复起）
Assemble()  按 WantsTmp() 分派到 AssembleLegacy / AssembleTmp
AssembleLegacy()  目标形态 Legacy：认领节点上已有的 Legacy（其次 TMP，B1 过渡态）；都没有则补一个 Legacy → FinishAssemble
AssembleTmp()  目标形态 TMP：节点上已有 TMP 就直接用（B1 过渡态，别重复换）；只有 Legacy 时进换组件流程；两者都没有则直接挂 TMP
WantsTmp()  static；GlobalDataMgr.GetTextType() == TextType.TMP（形态的真源，冷启动读一次即定）
FinishAssemble()  装配收尾：ready = true → ApplyStyle() → ApplyPendingOrConfigured()（各条路径共用这一份，避免漂移）
SwapToTmp()  IEnumerator；换组件顺序不可变：① legacy.enabled = false → ② Destroy 卸载 → ③ yield 一帧 → ④ AddComponent<TextMeshProUGUI> → ⑤ ApplySeed 播种 → ⑥ FinishAssemble
ApplyPendingOrConfigured()  有 pending → 按 pendingIsKey 走 SetTextKey / SetText；否则 !externallyDriven → ApplyConfiguredText()，否则 ApplyRawText(text)
SetText(value)  写运行期字面量（唯一入口）；装配前先缓冲；置 externallyDriven
SetTextKey(key)  写静态文案 key；清 externallyDriven
Refresh()  按 key / 字面量重取；externallyDriven 时跳过；**装配未完成时也直接 return**（收尾会统一应用 pending / 配置文案，此处再做一次只会写进空控件）（**当前无调用方**：语言是冷启动口径，没有 LanguageChangedEvent 来驱动它）
ApplyConfiguredText()  有 key → 查 LanguageData 取当前语言（回退链只有一份实现，在 GlobalDataMgr.PickClipText）；无 key → 用字面量
ApplyRawText(value)  记 rawText → 按需排版 → WriteToTarget →（Legacy + 中文排版时）起标点避头协程；标点避头是 Legacy 专用，用 legacyText != null 把 TMP 排除
WriteToTarget(display)  写进 legacyText 或 tmpText
ApplyStyle()  下发字体 / 字号 / 颜色 / 字重 / 对齐（两形态顺序一致：先换字体，字重才落在正确字体上）；Legacy 走 ApplyLegacyFont，TMP 走 ApplyTmpFont
ApplyLegacyFont(target)  B3：GlobalAssetsMgr.GetFontByFontId(fontId) → target.font；**f == null 时不赋值**（失败降级，见 Notes）
ApplyTmpFont(target)  B4：GlobalAssetsMgr.GetTmpFontAssetByFontId(fontId) → target.font；**font == null 时不赋值**（与 Legacy 同口径，见 Notes）
ToTmpFontStyle(style)  static；FontStyle → FontStyles（Bold / Italic / BoldAndItalic / Normal）
ApplyLegacyAlignment(target) / ApplyTmpAlignment(target)  11 种 OverrideAlignment → 对应枚举；Auto 与 Justified 在 Legacy 下不改，**Auto 在 TMP 下也不改**（改由播种值兜住）
IsDropdownTextNode(node)  static；向上 GetComponentsInParent<Dropdown>(true) 比对 captionText / itemText，命中则该节点必须保持 Legacy（见 Notes）
LegacySeed  struct；被换掉的 Legacy 的"节点既有设置"快照：wordWrap / overflow / raycast / richText / alignment
CaptureSeed(dying)  static；抄出上面 5 个字段，**必须在卸载之前调用**（Destroy 帧末生效，之后那个引用就是"假 null"、字段全读不到）
ApplySeed(target, seed)  static；把种子写进新挂的 TMP：enableWordWrapping / overflowMode / raycastTarget / richText / alignment（**richText 也必须搬**：AI 气泡正文靠 Legacy 的 `supportRichText = false` 要求纯文本，而 TMP 的 `richText` 默认 true，不搬就会把 AI 输出里的 `<xxx>` 当标签吞掉）
ToTmpAlignment(anchor)  static；TextAnchor → TextAlignmentOptions（9 宫格显式对照，TMP 枚举底层值不连续不能整数运算）
BuildCjkDisplay(source)  static；先剥旧标记再排版：空格转 NBSP、换行后补 8 个 NBSP、首行 8 个
StripIndent(s)  static；幂等剥掉本组件写过的缩进（防叠加）
Remove(s) / RemoveNewlines(s)  static；排版还原 / 去掉换行
SetTextOn(Text|TextMeshProUGUI, value)  static；写节点文案：节点上有 TextComponent 就走组件（享受缓冲 / 排版 / 多语言），没有则直写（迁移后应只剩 InputField 例外）
SetColorOn(Text, color)  static；写节点颜色：节点上有 TextComponent 就走 ColorValue（由 ApplyStyle 下发到当前认领的控件），没有则直写。**面板字段是 Text 类型、不能改（改了几十个 prefab 的引用会失效）；而 TMP 形态下那个 Legacy 已被卸载（当前认领的是 TMP）⇒ 直写 `target.color` 不是 no-op 就是写不到可见控件，所以调用方必须改为引用组件、走 `comp.ColorValue`**
ReadRaw(Text|TextMeshProUGUI)  static；读原文（有组件时返回 RawText，避免拿到 NBSP 缩进等排版标记）；传入 null / 已销毁引用返回空串
NodeOf(Text|TextMeshProUGUI, comp)  static；取文本节点（组件优先 —— TMP 形态下 Legacy 字段是"假 null"，用它取 `.transform` / `.parent` 会静默失效或抛）；字段可用时才退回字段，都没有返回 null
AvoidLeadingPunctuation()  IEnumerator；等一帧拿布局，把落在行首的标点插到上一行末；一次只挪一个、插完重排；未激活节点不能起协程

Notes:
- 继承 MonoBehaviour 而非 ComponentBase：基类链上的 UIBase 带 [RequireComponent(typeof(CanvasGroup))]，会给几十个纯文本节点各强加一个 CanvasGroup，并被登记进 PanelBase 的组件表（语义不对）。
- 写入缓冲是必需的：面板 OnViewBound 早于本组件装配，未 ready 的写入若直接丢就丢文案。
- 回退链设计为**永不返回空白**：key 未登记 → 字面量快照 → key 本身，并各打一条 Log.Warning。
- B1 刻意不碰字体（不清空 prefab 上已有的 font 引用）；B3 起 Legacy 形态按 fontId 下发字体；B4 起形态切换与 TMP 字体下发都已实现（形态由 SystemData.textType 决定）。
- **字体下发的降级路径**：ApplyLegacyFont 只在 GetFontByFontId 返回非 null 时写 target.font。取不到（字体包缺失 / 未预加载 / 预加载还在途 / 清单里没这个 fontId）就保持节点上已有的字体不动。**绝不能拿 null 去赋值** —— 那会让文本直接消失，而"字体包没了文本也照常显示"正是字体清单只存路径、不持引用这套设计要保住的底线。TMP 侧 ApplyTmpFont 与之**完全同口径**（font == null 绝不赋值，保持 TMP 自己的默认字体 —— 拿 null 覆盖 tmpText.font 会让文本直接消失）。
- 字体下发排在字号 / 颜色 / 字重之前：字体换了之后字重等属性才落在正确的字体上。
- TMP 分支下发的顺序与 Legacy 一致（字体 → 字号 → 颜色 → 字重 → 对齐）：B4 起 TMP 也按 fontId 下发字体（ApplyTmpFont → GetTmpFontAssetByFontId），不再是"不碰 tmpText.font"。
- **`m_RichText` 与 `horizontalOverflow` 不设字段**：实测 83/85 为默认开启、2 个例外都是 InputField 正文（不走本组件），故不迁移。
- 对齐枚举与 `TextAnchor` **同序**（UpperLeft…LowerRight = 0…8），迁移工具可直接复用原 `m_Alignment` 的整数值。
- **形态由 `SystemData.textType` 决定**（与语言同属设置类数据，冷启动读一次即定，不做运行期热切）：Legacy 认领节点上已有的 Legacy；TMP 优先用节点上已有的 TMP，只有 Legacy 时才换组件。
- **换组件顺序不可变：① 先禁用 → ② `Destroy` 卸载 → ③ 等一帧 → ④ 挂 TMP → ⑤ 播种 → ⑥ 收尾。** 必须先禁用：Legacy Text 与 TMP_Text 都继承 Graphic、共用同一个 CanvasRenderer，而 `Destroy` 到帧末才生效，光卸载挡不住同帧叠字。**必须卸载**：Unity 不允许同一 GameObject 上存在两个 Graphic，把 Legacy 留着（哪怕已 `enabled = false`）会让 `AddComponent<TextMeshProUGUI>()` **被拒绝并返回 null**（实测原话：`Can't add 'TextMeshProUGUI' to X because a 'Text' is already added to the game object!`），紧接着访问就 NRE。等一帧是为了让卸载生效、CanvasRenderer 空出来。写入缓冲（ready == false）已配套实现。
- **调用方契约：必须引用本组件，不能只持有 `Text`。** 卸载 Legacy 后，所有"持有 `Text` 字段"的调用点都变"假 null"，而静态入口第一道闸是 `if (target == null) return;` ⇒ **静默 no-op**（不抛异常、最难查）：`SetTextOn(字段, …)` 一个字都不写、`ReadRaw(字段)` 返回空串、`字段.gameObject.SetActive(...)` 被跳过、`SetColorOn(字段, …)` 无效。⇒ MonoBehaviour 在 `Awake`、普通类在构造 / 首次拿到 `Text` 的那一处解析并缓存 `TextComponent`，写走 `comp.SetText`、读走 `comp.RawText`、颜色走 `comp.ColorValue`；只有节点上本就没有组件（InputField 等例外节点）才退回静态入口。**绝不可在调用时才 `GetComponent`**（那时 Legacy 已卸载）。全工程 59 处调用点已按此改造。
- **调用方"不需要自己等初始化"**：装配未完成时 `SetText` 先落进 `pending` 缓冲、`FinishAssemble` 统一应用 —— 这就是"引用组件后也要等初始化好再赋值"的落点；只要持有组件，赋值时机随意。
- **别把 TMP 挂到子节点来回避卸载**：那会让同节点的 `ContentSizeFitter` 失去可测量的 Graphic（全工程 11 个 `TextComponent` 节点带 `ContentSizeFitter`、4 个带 `LayoutElement`，会塌）。
- **未激活层级下协程不推进**（起协程还会直接报错）：面板与展示实例常以 inactive 实例化，故 Assemble 遇到"要换形态但 !isActiveAndEnabled"时只置 swapPending 标记、不起协程，由 OnEnable 补跑；swapping 保证协程在跑时 OnEnable 不重复起。
- **Dropdown 例外靠代码自动识别，不加序列化字段、不改 prefab**：UGUI Dropdown 只认 Legacy Text（captionText / itemText），这两类节点换 TMP 会让 Dropdown 失去文本目标。用"向上找 Dropdown 并比对引用"（IsDropdownTextNode）自动识别，任何新增的 Dropdown 都自动免疫；命中 → 保持 Legacy，并只打一条 Log.Verbose（命中是预期行为、不是错误，故不刷 Warning）。
- **装配期间的写入缓冲跨帧生效**：B4 起 ready == false 至少跨一帧（见换组件第 ② 步），SetText / SetTextKey 的 pending 缓冲与 Refresh 的 `if (!ready) return` 必须同时成立，否则这一帧的写入会丢。
- **播种是"Legacy 所见即 TMP 所得"的保证**：ApplyStyle 只下发显式字段，且对齐为 Auto 时故意不改 —— 不播种的话这类节点会从"原对齐"掉到 TMP 默认的左上对齐，属视觉回归。播 4 项：enableWordWrapping ← horizontalOverflow == Wrap；overflowMode ← verticalOverflow == Truncate ? Truncate : Overflow；raycastTarget ← Legacy 的 raycastTarget（**必须搬，否则可点文本会失去点击**）；alignment ← Legacy 的 TextAnchor（作为 Auto 的种子）。
- **播种值必须在卸载之前抄出来**（CaptureSeed）：`Destroy` 帧末生效，协程恢复时那个引用已是"假 null"、读不到任何字段。
- **Auto 的种子选"换组件时一次性播种"，而不是给 ApplyTmpAlignment 的 default 分支加种子字段**：后者要多一个"是否刚换过组件"的状态来区分 B1 过渡态（节点上本来就有 TMP，不该被播种值覆盖），多一个可能失效的标记；播种只发生在换组件那一刻，抄出即用、用完即弃，语义最窄，也不必改动 ApplyTmpAlignment 的既有分支。
- **TMP 的断行与避头按 §3 已知取舍接受**：NBSP 缩进（BuildCjkDisplay）两种形态都适用；标点避头（AvoidLeadingPunctuation）依赖 TextGenerator 的行布局，是 Legacy 专用，不做 TMP 版。
- **两者都没有时直接挂 TMP，而不是先补一个 Legacy 再销毁**：那种节点没有可播种的"节点既有设置"，先加后毁只会白跨一帧；B1 的"补一个 Legacy"兜底只在目标形态是 Legacy 时保留。
- **字体不搬 fontSize / color / fontStyle**：它们是本组件的显式字段，ApplyStyle 一定会覆盖，搬过来只会被立刻冲掉。
