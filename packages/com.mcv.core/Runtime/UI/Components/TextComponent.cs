using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.System;
using MCV_Module.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Components
{
    /// <summary>
    /// 全工程唯一文本写入路径：持有字体 id / 字号 / 颜色 / 对齐 / 多语言 key，按当前形态下发到 Legacy Text 或 TMP。
    ///
    /// <para><b>为什么继承 MonoBehaviour 而不是 ComponentBase</b>：基类链上的 UIBase 带
    /// <c>[RequireComponent(typeof(CanvasGroup))]</c>，本组件要挂到几十个纯文本节点上，继承会给每个节点强加一个
    /// CanvasGroup，也会被登记进 PanelBase 的组件表（语义不对：文本节点不是面板业务组件）。</para>
    ///
    /// <para><b>形态</b>：由 <c>SystemData.textType</c> 决定（设置类数据，冷启动读一次即定，不做运行期热切）。Legacy 形态认领
    /// 节点上已有的 Legacy Text（都没有则补一个）；TMP 形态优先直接用节点上已有的 TMP，只有 Legacy 时按"先禁用 → 卸载 →
    /// 等一帧 → 挂 TMP → 播种 → 收尾"换组件（顺序不可变，见 <see cref="SwapToTmp"/>）。</para>
    ///
    /// <para><b>调用方契约（B4 起）</b>：换 TMP 会**卸载**节点上的 Legacy Text，于是"持有 Text 字段"的调用点全变"假 null"、
    /// 静态入口静默 no-op ⇒ 调用方必须**引用本组件**（`Awake` / 构造时解析并缓存），写文本走 <c>SetText</c>、读走 <c>RawText</c>、
    /// 写颜色等非文本成员走 <c>ColorValue</c>。装配未完成时 <c>SetText</c> 会先进 <c>pending</c> 缓冲、装配完成统一应用，
    /// 故调用方**不需要自己等初始化**，但**必须持有组件**。UGUI Dropdown 的 caption / item 文本节点自动保持 Legacy ——
    /// 换 TMP 会让 Dropdown 失去文本目标。**字体**：两种形态都从字体全局包同步取件下发（Legacy 走 <see cref="ApplyLegacyFont"/>、
    /// TMP 走 <see cref="ApplyTmpFont"/>）。<b>失败降级</b>：取不到字体时**不赋值**，保持 prefab 上已有的字体 ——
    /// 字体包缺 / 未预加载时文本不能消失。</para>
    /// </summary>
    public class TextComponent : MonoBehaviour
    {
        #region 序列化字段
        /// <summary>字体 id（稳定字符串，对应 FontCatalogSO 的条目 id）；两种形态都生效，取不到则保持节点原有字体。</summary>
        [SerializeField] string fontId = "ui";

        /// <summary>字面量文案（无 languageKey 时显示它；也是 Inspector 里的可读快照）。</summary>
        [SerializeField, TextArea(3, 8)] string text = "";

        /// <summary>多语言 key（对应 LanguageClip.id）；有值时优先于字面量。</summary>
        [SerializeField] string languageKey = "";

        [SerializeField] int fontSize = 16;

        /// <summary>字重（Legacy <see cref="FontStyle"/> ↔ TMP <c>FontStyles</c>）。实测 23 个节点是 Bold，必须有落点。</summary>
        [SerializeField] FontStyle fontStyle = FontStyle.Normal;
        [SerializeField] Color color = Color.black;

        /// <summary>对齐（9 宫格 + Justified + Auto）；Auto = 不改节点原有对齐。</summary>
        [SerializeField] OverrideAlignment alignment = OverrideAlignment.Auto;

        /// <summary>逐节点开中文排版（NBSP 缩进 + 标点避头），默认关。</summary>
        [SerializeField] bool cjkTypography = false;
        #endregion

        #region 运行时状态
        /// <summary>当前生效的 Legacy 文本控件（目标形态为 Legacy 时优先认领节点上已有的）；与 <see cref="tmpText"/> 至多一个非空。</summary>
        Text legacyText;
        /// <summary>当前生效的 TMP 文本控件（目标形态为 TMP 时才有值）。</summary>
        TextMeshProUGUI tmpText;

        /// <summary>装配是否完成；未完成时到达的写入先存 pending（面板 OnViewBound 可能早于本组件的装配，且换组件还要跨帧）。</summary>
        bool ready;

        /// <summary>换组件协程是否在跑；OnEnable 用它防重入（协程在跑时不再重复起）。</summary>
        bool swapping;

        /// <summary>要换形态但起不了协程（未激活层级）：置此标记，等 OnEnable 补跑。</summary>
        bool swapPending;

        /// <summary>待应用文案（按 key 还是按字面量）。</summary>
        string pendingValue;
        bool pendingIsKey;

        /// <summary>外部显式写过文案 ⇒ 本组件不再用 languageKey/字面量覆盖它（语言切换时让位）。</summary>
        bool externallyDriven;

        /// <summary>当前**原文**（不含 NBSP 缩进等排版标记），供外部读回。</summary>
        string rawText = string.Empty;
        #endregion

        #region 只读视图（Editor 工具与校验用）
        /// <summary>字体 id（Editor 工具读取用）。</summary>
        public string FontId => fontId;
        /// <summary>多语言 key（Editor 工具读取用）。</summary>
        public string LanguageKey => languageKey;
        /// <summary>是否开了中文排版。</summary>
        public bool CjkTypography => cjkTypography;
        /// <summary>当前原文（不含排版标记）。</summary>
        public string RawText => rawText;
        /// <summary>字号（读写）。</summary>
        public int FontSizeValue { get => fontSize; set { fontSize = value; ApplyStyle(); } }
        /// <summary>字重（读写）。</summary>
        public FontStyle FontStyleValue { get => fontStyle; set { fontStyle = value; ApplyStyle(); } }
        /// <summary>颜色（读写）。</summary>
        public Color ColorValue { get => color; set { color = value; ApplyStyle(); } }
        /// <summary>对齐（读写）；Auto 表示不改。</summary>
        public OverrideAlignment Alignment { get => alignment; set { alignment = value; ApplyStyle(); } }
        #endregion

        #region 生命周期
        // WHY: 语言与文本形态都在 Awake 读一次（ApplyConfiguredText → GlobalDataMgr.PickClipText；形态 → GlobalDataMgr.GetTextType）。
        // 两者都是设置类数据、冷启动才变，故本组件不做运行期热切：每个面板实例在自己的 Awake 里按当时的语言定位到中/英槽位
        // （英文槽位为空则回退中文），按当时的形态决定用 Legacy 还是 TMP。
        void Awake()
        {
            // WHY: 这里不单独消费 pending —— 装配能立即完成的路径（Legacy / 节点上已有 TMP / Dropdown 例外）都由
            // FinishAssemble 统一收尾；需要换组件时 ready 仍为 false，写入继续留在 pending 里，等协程收尾时按同一份逻辑补应用。
            Assemble();
        }

        // WHY: 面板与展示实例常以 inactive 实例化，而未激活的 MonoBehaviour 起不了协程（Unity 会直接报错）。
        // 所以换形态只能在这里补跑：待换标记还在、仍未 ready、且目标形态仍是 TMP 时才起协程。
        void OnEnable()
        {
            if (ready || swapping || !swapPending) return;
            // WHY: 形态是冷启动数据、正常不会在运行期变；真变了就退回重新装配一次，不留"待换"这种半截状态。
            if (!WantsTmp()) { Assemble(); return; }
            StartCoroutine(SwapToTmp());
        }

        /// <summary>目标形态是否为 TMP（设置类数据，冷启动读一次即定）。</summary>
        static bool WantsTmp() => GlobalDataMgr.GetTextType() == TextType.TMP;

        /// <summary>把节点上的文本控件装配成目标形态并下发样式（目标形态见 <see cref="WantsTmp"/>）。</summary>
        void Assemble()
        {
            if (WantsTmp()) AssembleTmp();
            else AssembleLegacy();
        }

        /// <summary>目标形态 Legacy：认领节点上已有的 Legacy（其次 TMP，B1 过渡态）；都没有则补一个 Legacy —— 与 B1 口径一致。</summary>
        void AssembleLegacy()
        {
            legacyText = GetComponent<Text>();
            tmpText = legacyText != null ? null : GetComponent<TextMeshProUGUI>();
            if (legacyText == null && tmpText == null)
            {
                legacyText = gameObject.AddComponent<Text>();
            }
            FinishAssemble();
        }

        /// <summary>
        /// 目标形态 TMP：节点上已有 TMP 就直接用它（B1 过渡态，别重复换）；只有 Legacy 时走换组件流程；
        /// 两者都没有时直接挂一个 TMP（没有可播种的"节点既有设置"，也没必要先加一个 Legacy 再销毁它）。
        /// </summary>
        void AssembleTmp()
        {
            tmpText = GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                legacyText = null;
                FinishAssemble();
                return;
            }

            legacyText = GetComponent<Text>();
            if (legacyText == null)
            {
                tmpText = gameObject.AddComponent<TextMeshProUGUI>();
                FinishAssemble();
                return;
            }

            if (IsDropdownTextNode(legacyText))
            {
                // WHY: 命中是**预期行为**（Dropdown 本来就该用 Legacy），不是配置错误，所以走 Verbose 而不是 Warning ——
                // 免得每个 Dropdown 文本节点每次 Awake 都往控制台刷一条警告。
                Log.Verbose($"[TextComponent] 「{name}」是 Dropdown 的 caption/item 文本节点，保持 Legacy 形态");
                tmpText = null;
                FinishAssemble();
                return;
            }

            // WHY: 未激活层级下协程不推进（而且起协程会直接报错）—— 不起协程，只置"待换"标记，等 OnEnable 补跑。
            if (isActiveAndEnabled) StartCoroutine(SwapToTmp());
            else swapPending = true;
        }

        /// <summary>装配收尾（各条路径共用一份，避免漂移）：置 ready → 下发样式 → 消费装配期间到达的写入。</summary>
        void FinishAssemble()
        {
            ready = true;
            swapPending = false;
            ApplyStyle();
            ApplyPendingOrConfigured();
        }

        /// <summary>
        /// 把节点上的 Legacy Text 换成 TMP。顺序不可变：① 先禁用 → ② 卸载 → ③ 等一帧 → ④ 挂 TMP → ⑤ 播种 → ⑥ 收尾。
        /// ⚠️ 必须**卸载**而不能只禁用：Unity 不允许同一 GameObject 上存在两个 Graphic，`AddComponent<TextMeshProUGUI>()` 会被拒绝并返回 null。
        /// </summary>
        IEnumerator SwapToTmp()
        {
            swapping = true;
            swapPending = false;

            Text dying = legacyText;
            legacyText = null;
            if (dying == null)
            {
                // 理论不可达（只在 legacyText 非空时才起本协程）；真到了就退化成"直接挂 TMP"，不留半截状态。
                tmpText = gameObject.AddComponent<TextMeshProUGUI>();
                swapping = false;
                FinishAssemble();
                yield break;
            }

            // WHY: 播种值必须在**卸载之前**抄出来 —— 一旦卸载，dying 就成了"假 null"，字段全读不到。
            LegacySeed seed = CaptureSeed(dying);

            // WHY: ① 必须先禁用再卸载。Legacy Text 与 TMP_Text 都继承 Graphic、共用同一个 CanvasRenderer，
            // 同帧同时 enabled 会叠字；而 Destroy 到帧末才生效，光卸载挡不住同帧叠字。
            // WHY(为什么必须卸载、不能只禁用)：Unity 不允许同一个 GameObject 上存在两个 Graphic —— 实测
            // `AddComponent<TextMeshProUGUI>()` 在该节点还有 Text / Image 时会被拒绝并**返回 null**（Unity 原话：
            // "Can't add 'TextMeshProUGUI' to X because a 'Text' is already added to the game object!"），紧接着访问它就 NRE。
            // 所以"保留 Legacy 只禁用"这条捷径走不通，必须把 Legacy 真正卸掉，节点才空得出 Graphic 槽位。
            dying.enabled = false;                                   // ① 先禁用
            Destroy(dying);                                          // ② 再卸载（帧末生效）
            yield return null;                                       // ③ 等一帧让卸载生效、CanvasRenderer 空出来

            tmpText = gameObject.AddComponent<TextMeshProUGUI>();    // ④ 挂 TMP
            ApplySeed(tmpText, seed);                                // ⑤ 先播种"节点既有设置"，再由 ApplyStyle 的显式字段覆盖
            swapping = false;
            FinishAssemble();                                        // ⑥ ready = true → ApplyStyle() → 消费装配期间的写入
        }

        // ⚠️ 卸载带来的**调用方契约**（B4 起）：Legacy 被卸载后，所有"持有 Text 字段"的调用点都变成"假 null"，
        // 静态入口 `SetTextOn` / `ReadRaw` / `SetColorOn` 会在 `target == null` 处直接 return —— **静默 no-op**。
        // 故调用方必须**引用 TextComponent**：在 `Awake`（MonoBehaviour）或构造时（普通类）解析一次并缓存，之后
        // 写文本走 `comp.SetText` / 读走 `comp.RawText` / 写颜色等非文本成员走 `comp.ColorValue`。
        // **不需要自己等初始化**：装配未完成时 `SetText` 会先落进 pending 缓冲，`FinishAssemble` 时统一应用 ——
        // 这正是"引用 TextComponent 的也要等初始化好了再赋值"的落点。

        /// <summary>消费装配期间缓冲的写入；没有缓冲则按配置取文案。装配立即完成与换组件收尾共用这一份，避免两处漂移。</summary>
        void ApplyPendingOrConfigured()
        {
            if (pendingValue != null)
            {
                string value = pendingValue;
                bool isKey = pendingIsKey;
                pendingValue = null;
                if (isKey) SetTextKey(value); else SetText(value);
                return;
            }
            if (!externallyDriven) ApplyConfiguredText();
            // WHY: 外部驱动过（写过字面量）就不能再用配置文案盖掉它；把装配前写的那个字面量补写进去。
            else ApplyRawText(text);
        }
        #endregion

        #region 对外 API（唯一写入路径）
        /// <summary>写入字面量文案（运行期数据走这里）。</summary>
        public void SetText(string value)
        {
            if (!ready)
            {
                pendingValue = value;
                pendingIsKey = false;
                return;
            }
            externallyDriven = true;
            text = value ?? string.Empty;
            ApplyRawText(text);
        }

        /// <summary>写入多语言 key（静态文案走这里）；取不到时按回退链显示。</summary>
        public void SetTextKey(string key)
        {
            if (!ready)
            {
                pendingValue = key;
                pendingIsKey = true;
                return;
            }
            externallyDriven = false;
            languageKey = key ?? string.Empty;
            ApplyConfiguredText();
        }

        /// <summary>按 languageKey / 字面量重新取一次文案（语言切换时由管理器调用）。</summary>
        public void Refresh()
        {
            // WHY: 装配（含跨帧的换组件）还没完成时直接返回 —— 收尾时会统一应用 pending / 配置文案，这里再做一次只会写进空控件。
            if (!ready) return;
            if (externallyDriven) return;
            ApplyConfiguredText();
        }

        /// <summary>当前显示用的原文（不含排版标记）。</summary>
        public string TextValue => rawText;
        #endregion

        #region 静态过渡入口（面板持有的是 Text/TMP 引用时用）
        // WHY: 面板的 Inspector 字段是 `Text` 类型，改成 TextComponent 会让所有 prefab 上的引用失效（要重绑几十处）。
        // 所以保留字段类型，写入统一走这两个静态入口：节点上有组件就走组件（享受缓冲/排版/多语言），没有则直写兜底。
        /// <summary>写 Legacy 文本节点；节点上无 TextComponent 时直写（迁移后应只剩 InputField 例外）。</summary>
        public static void SetTextOn(Text target, string value)
        {
            if (target == null) return;
            if (target.TryGetComponent(out TextComponent comp)) { comp.SetText(value); return; }
            target.text = value;
        }

        /// <summary>写 TMP 文本节点；节点上无 TextComponent 时直写。</summary>
        public static void SetTextOn(TextMeshProUGUI target, string value)
        {
            if (target == null) return;
            if (target.TryGetComponent(out TextComponent comp)) { comp.SetText(value); return; }
            target.text = value;
        }

        // WHY: 不能只写 `target.color = ...` —— TMP 形态下 SwapToTmp 会把节点上的 Legacy Text **卸载**（必须卸：同节点不能有两个 Graphic），
        // 于是 `target` 成了"假 null"、直写会抛 `MissingReferenceException`；而本组件当前认领的控件已换成 TMP，颜色得由它下发。
        // 所以调用方必须持有组件：`ColorValue` 写的是组件自己的配置色，`ApplyStyle` 会把它下发到当前认领的控件上，Legacy / TMP 两种形态都正确。
        /// <summary>写 Legacy 文本节点的颜色；节点上有 TextComponent 就走组件（换形态后仍有效），否则直写。</summary>
        public static void SetColorOn(Text target, Color color)
        {
            if (target == null) return;
            if (target.TryGetComponent(out TextComponent comp)) { comp.ColorValue = color; return; }
            target.color = color;
        }

        /// <summary>读节点上的原文（有组件时返回 RawText，避免拿到 NBSP 缩进等排版标记）。</summary>
        public static string ReadRaw(Text target)
        {
            if (target == null) return string.Empty;
            return target.TryGetComponent(out TextComponent comp) ? comp.RawText : target.text;
        }

        /// <summary>读 TMP 节点上的原文。</summary>
        public static string ReadRaw(TextMeshProUGUI target)
        {
            if (target == null) return string.Empty;
            return target.TryGetComponent(out TextComponent comp) ? comp.RawText : target.text;
        }

        // WHY: 为什么需要它 —— TMP 形态下节点上的 Legacy Text 已被卸载，面板上的 `Text` 字段随即成"假 null"，
        //       任何 `字段.transform` / `字段.parent` 取节点的写法都会静默失效（不抛异常、最难查）；布局重建、
        //       取父级、取 rectTransform 都应经它解析，只有节点上压根没有组件时才退回字段。
        /// <summary>文本节点：优先取组件所在节点（组件在就说明它是当前认领方），字段可用时才退回字段，都没有返回 null。</summary>
        public static Transform NodeOf(Text legacy, TextComponent comp)
        {
            if (comp != null) return comp.transform;
            return legacy != null ? legacy.transform : null;
        }

        /// <summary>TMP 版 <see cref="NodeOf(Text, TextComponent)"/>。</summary>
        public static Transform NodeOf(TextMeshProUGUI legacy, TextComponent comp)
        {
            if (comp != null) return comp.transform;
            return legacy != null ? legacy.transform : null;
        }
        #endregion

        #region 取文案与下发
        /// <summary>按 languageKey 取文案；无 key 用字面量。回退链的实现只有一份（GlobalDataMgr.PickClipText）。</summary>
        void ApplyConfiguredText()
        {
            if (string.IsNullOrEmpty(languageKey))
            {
                ApplyRawText(text);
                return;
            }

            GlobalDataMgr mgr = GlobalDataMgr.Instance;
            if (mgr == null || !mgr.TryGetClip(languageKey, out LanguageClip clip))
            {
                // WHY: 回退链的第二段——key 不存在时显示字面量快照，再退到 key 本身，永不显示空白。
                Log.Warning($"[TextComponent] 语言 key「{languageKey}」未在 LanguageData 中登记（{name}）", this);
                ApplyRawText(string.IsNullOrEmpty(text) ? languageKey : text);
                return;
            }

            string picked = mgr.PickClipText(clip);
            if (picked == null)
            {
                Log.Warning($"[TextComponent] 语言 key「{languageKey}」当前语言槽位为空（{name}）", this);
                picked = string.IsNullOrEmpty(text) ? languageKey : text;
            }
            ApplyRawText(picked);
        }

        /// <summary>把原文（按需排版）写进当前文本控件。</summary>
        void ApplyRawText(string value)
        {
            rawText = value ?? string.Empty;
            string display = cjkTypography ? BuildCjkDisplay(rawText) : rawText;
            WriteToTarget(display);
            if (cjkTypography && legacyText != null)
            {
                // WHY: 标点避头是 **Legacy 专用** —— 它依赖 TextGenerator 的行布局，TMP 没有等价物（TMP 的断行差异按 §3 的已知
                // 取舍接受，不做 TMP 版避头），故这里用 legacyText != null 把 TMP 形态排除在外；NBSP 缩进（BuildCjkDisplay）
                // 两种形态都适用，不受影响。标点避头还需等一帧拿到布局结果；未激活的节点起不了协程（Unity 会报错），先判 isActiveAndEnabled。
                if (isActiveAndEnabled) StartCoroutine(AvoidLeadingPunctuation());
            }
        }

        /// <summary>把显示文写进当前控件（Legacy 与 TMP 分别写）。</summary>
        void WriteToTarget(string display)
        {
            if (legacyText != null) legacyText.text = display;
            else if (tmpText != null) tmpText.text = display;
        }

        /// <summary>下发样式：字体 / 字号 / 颜色 / 字重 / 对齐（字体见 <see cref="ApplyLegacyFont"/> 与 <see cref="ApplyTmpFont"/>）。</summary>
        void ApplyStyle()
        {
            if (legacyText != null)
            {
                ApplyLegacyFont(legacyText);
                legacyText.fontSize = fontSize;
                legacyText.color = color;
                legacyText.fontStyle = fontStyle;
                ApplyLegacyAlignment(legacyText);
            }
            else if (tmpText != null)
            {
                // WHY: 顺序与 Legacy 分支完全一致（字体 → 字号 → 颜色 → 字重 → 对齐）：先把字体换掉，字重才落在正确的字体上。
                // 对齐为 Auto 时 ApplyTmpAlignment 不写，节点原有对齐由 ApplySeed 的播种值保证。
                ApplyTmpFont(tmpText);
                tmpText.fontSize = fontSize;
                tmpText.color = color;
                tmpText.fontStyle = ToTmpFontStyle(fontStyle);
                ApplyTmpAlignment(tmpText);
            }
        }

        // WHY: 失败降级是这里的**核心语义** —— f == null 时绝对不赋值：字体包缺 / 还没预加载完时，
        // 拿 null 覆盖 legacyText.font 会让文本直接消失；保持 prefab 上已有的字体，最坏只是字体不是配置里那一款。
        /// <summary>把 fontId 对应的 Legacy 字体下发到控件（取不到则保持节点原有字体，见上）。</summary>
        void ApplyLegacyFont(Text target)
        {
            Font font = GlobalAssetsMgr.GetFontByFontId(fontId);
            if (font != null) target.font = font;
        }

        // WHY: 与 Legacy 同口径 —— font == null 时绝不赋值，保持 TMP 自己的默认字体；
        // 拿 null 覆盖 tmpText.font 会让文本直接消失。
        /// <summary>把 fontId 对应的 TMP 字体资产下发到控件（取不到则保持节点原有字体，见上）。</summary>
        void ApplyTmpFont(TextMeshProUGUI target)
        {
            TMP_FontAsset font = GlobalAssetsMgr.GetTmpFontAssetByFontId(fontId);
            if (font != null) target.font = font;
        }

        /// <summary>Legacy <see cref="FontStyle"/> → TMP <c>FontStyles</c> 的等价映射。</summary>
        static FontStyles ToTmpFontStyle(FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Bold: return FontStyles.Bold;
                case FontStyle.Italic: return FontStyles.Italic;
                case FontStyle.BoldAndItalic: return FontStyles.Bold | FontStyles.Italic;
                default: return FontStyles.Normal;
            }
        }

        void ApplyLegacyAlignment(Text target)
        {
            switch (alignment)
            {
                case OverrideAlignment.UpperLeft: target.alignment = TextAnchor.UpperLeft; break;
                case OverrideAlignment.UpperCenter: target.alignment = TextAnchor.UpperCenter; break;
                case OverrideAlignment.UpperRight: target.alignment = TextAnchor.UpperRight; break;
                case OverrideAlignment.MiddleLeft: target.alignment = TextAnchor.MiddleLeft; break;
                case OverrideAlignment.MiddleCenter: target.alignment = TextAnchor.MiddleCenter; break;
                case OverrideAlignment.MiddleRight: target.alignment = TextAnchor.MiddleRight; break;
                case OverrideAlignment.LowerLeft: target.alignment = TextAnchor.LowerLeft; break;
                case OverrideAlignment.LowerCenter: target.alignment = TextAnchor.LowerCenter; break;
                case OverrideAlignment.LowerRight: target.alignment = TextAnchor.LowerRight; break;
                // WHY: Legacy TextAnchor 没有两端对齐；Auto / Justified 一律保持节点原有值。
                default: break;
            }
        }

        void ApplyTmpAlignment(TextMeshProUGUI target)
        {
            switch (alignment)
            {
                case OverrideAlignment.UpperLeft: target.alignment = TextAlignmentOptions.TopLeft; break;
                case OverrideAlignment.UpperCenter: target.alignment = TextAlignmentOptions.Top; break;
                case OverrideAlignment.UpperRight: target.alignment = TextAlignmentOptions.TopRight; break;
                case OverrideAlignment.MiddleLeft: target.alignment = TextAlignmentOptions.Left; break;
                case OverrideAlignment.MiddleCenter: target.alignment = TextAlignmentOptions.Center; break;
                case OverrideAlignment.MiddleRight: target.alignment = TextAlignmentOptions.Right; break;
                case OverrideAlignment.LowerLeft: target.alignment = TextAlignmentOptions.BottomLeft; break;
                case OverrideAlignment.LowerCenter: target.alignment = TextAlignmentOptions.Bottom; break;
                case OverrideAlignment.LowerRight: target.alignment = TextAlignmentOptions.BottomRight; break;
                case OverrideAlignment.Justified: target.alignment = TextAlignmentOptions.Justified; break;
                default: break;
            }
        }

        // WHY: UGUI Dropdown 只认 Legacy Text（captionText / itemText）—— 这两类节点换 TMP 会让 Dropdown 失去文本目标。
        // 用"向上找 Dropdown 并比对引用"来自动识别，而不是给节点加开关：任何新增的 Dropdown 都自动免疫。
        /// <summary>该 Legacy 节点是否是某个 Dropdown 的 caption / item 文本（是则不能换 TMP）。</summary>
        static bool IsDropdownTextNode(Text node)
        {
            if (node == null) return false;
            var owners = node.GetComponentsInParent<Dropdown>(true);
            for (int i = 0; i < owners.Length; i++)
                if (owners[i] != null && (owners[i].captionText == node || owners[i].itemText == node)) return true;
            return false;
        }

        /// <summary>被换掉的 Legacy 控件的"节点既有设置"快照（在 <see cref="ApplyStyle"/> 下发显式字段之前落到 TMP 上）。</summary>
        struct LegacySeed
        {
            public bool wordWrap;
            public TextOverflowModes overflow;
            public bool raycast;
            public bool richText;
            public TextAlignmentOptions alignment;
        }

        // WHY: 必须在"禁用 / 卸载"之前把值抄出来 —— `Destroy` 到帧末才生效，协程恢复时那个引用已是"假 null"、字段全读不到。
        /// <summary>抄出被换掉的 Legacy 的节点既有设置（换行 / 溢出 / 点击 / 富文本 / 对齐）。</summary>
        static LegacySeed CaptureSeed(Text dying)
        {
            return new LegacySeed
            {
                wordWrap = dying.horizontalOverflow == HorizontalWrapMode.Wrap,
                overflow = dying.verticalOverflow == VerticalWrapMode.Truncate ? TextOverflowModes.Truncate : TextOverflowModes.Overflow,
                raycast = dying.raycastTarget,
                richText = dying.supportRichText,
                alignment = ToTmpAlignment(dying.alignment),
            };
        }

        // WHY: 选"换组件时一次性播种"而不是给 ApplyTmpAlignment 的 default 分支加一个种子字段 —— 后者会引入一个需要区分
        // "是否刚换过组件"的额外状态（节点上本来就有 TMP 的 B1 过渡态**不该**被播种值覆盖），多一个可能失效的标记；
        // 而播种只发生在换组件那一刻，抄出即用、用完即弃，语义最窄，也不必改动 ApplyTmpAlignment 的既有分支。
        // WHY: ApplyStyle 只下发**显式字段**，且对齐为 Auto 时故意不改 —— 不播种的话这类节点会从"原对齐"掉到 TMP 默认的
        // 左上对齐，属视觉回归。所以这里先把 Legacy 的节点既有设置搬到 TMP 上，再由 ApplyStyle 用显式字段覆盖。
        /// <summary>把种子写进新挂的 TMP：换行 / 溢出 / 点击 / 富文本开关 + 节点原有对齐（保证"Legacy 所见即 TMP 所得"）。</summary>
        static void ApplySeed(TextMeshProUGUI target, LegacySeed seed)
        {
            target.enableWordWrapping = seed.wordWrap;
            target.overflowMode = seed.overflow;
            target.raycastTarget = seed.raycast;   // 必须搬：否则可点文本会失去点击
            // WHY: 也必须搬 —— AI 气泡正文靠 Legacy 的 `supportRichText = false` 要求纯文本（否则 AI 输出里的 <xxx> 会被当标签吞掉）。
            // TMP 的对应开关是 `richText`（同为"是否解析富文本"，语义一致、无需取反），而它默认是 true，不搬就等于丢掉了这条约束。
            target.richText = seed.richText;
            target.alignment = seed.alignment;     // alignment == Auto 时 ApplyTmpAlignment 不再覆盖，落的就是这个种子值
        }

        // WHY: 本工程实测 OverrideAlignment 前 9 项与 TextAnchor 同序（UpperLeft…LowerRight = 0…8），但 TextAlignmentOptions
        // 的底层值并不连续（TopLeft = Left | Top 这类位组合），不能靠整数运算转换，所以仍写一张显式对照表。
        /// <summary>Legacy <see cref="TextAnchor"/> → TMP <c>TextAlignmentOptions</c>（只覆盖 9 宫格）。</summary>
        static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.TopLeft;
            }
        }
        #endregion

        #region 中文排版（原 ChineseText 融入）
        /// <summary>不换行空格：缩进一律用它拼，普通空格会被排版还原逻辑替换掉。</summary>
        const string Nbsp = "\u00A0";

        static readonly Regex LeadingPunctuation =
            new Regex(@"(\！|\？|\，|\。|\《|\》|\）|\：|\”|\’|\、|\；|\+|\-|\.|\?)");

        /// <summary>原文 → 显示文（空格转 NBSP + 首行与换行后缩进）。</summary>
        static string BuildCjkDisplay(string source)
        {
            string s = source ?? string.Empty;
            // WHY: 幂等——已被排版的文本再进来会叠加缩进，先剥掉本轮标记。
            s = StripIndent(s);
            s = s.Replace(" ", Nbsp);
            s = s.Replace("\n", "\n" + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp);
            s = Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + s;
            return s;
        }

        /// <summary>剥掉本组件写过的缩进标记（幂等；不碰正文里的普通空格）。</summary>
        static string StripIndent(string s)
        {
            string eight = Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp + Nbsp;
            s = s.Replace("\n" + eight, "\n");
            if (s.StartsWith(eight)) s = s.Substring(eight.Length);
            return s;
        }

        /// <summary>把本组件写过的排版还原成原文（外部要拿原文时用）。</summary>
        public static string Remove(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Replace("\n\u3000\u3000", "\n");
            s = s.Replace("\u3000\u3000", "");
            return s.Replace(Nbsp, " ");
        }

        /// <summary>去掉所有换行符。</summary>
        public static string RemoveNewlines(string s) => s?.Replace("\n", string.Empty);

        /// <summary>标点避头：等一帧拿到布局，把落在行首的标点挪到上一行末（Legacy 专用，TMP 见类注释）。</summary>
        IEnumerator AvoidLeadingPunctuation()
        {
            yield return null;
            if (legacyText == null) yield break;

            TextGenerator generator = legacyText.cachedTextGenerator;
            if (generator == null || generator.lineCount <= 1) yield break;

            string current = legacyText.text;
            var sb = new StringBuilder(current);
            var lines = generator.lines;
            int inserted = 0;
            for (int i = 1; i < lines.Count; i++)
            {
                int index = lines[i].startCharIdx + inserted;
                if (index <= 0 || index >= sb.Length) continue;
                if (sb[index - 1] == '\n') continue;
                if (!LeadingPunctuation.IsMatch(sb[index].ToString())) continue;
                sb.Insert(index - 1, '\n');
                inserted++;
                break; // WHY: 一次只挪一个，插完重排再处理下一个，避免索引整体失效。
            }
            if (inserted == 0) yield break;

            legacyText.text = sb.ToString();
            if (isActiveAndEnabled) StartCoroutine(AvoidLeadingPunctuation());
        }
        #endregion
    }
}
