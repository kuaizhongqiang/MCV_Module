using System;
using System.Collections;
using System.Text;
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.System;
using MCV_Module.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Components
{
    /// <summary>全工程唯一文本写入路径：按 <c>SystemData.textType</c> 裁决的形态把文案 / 字体 / 样式下发到 Legacy Text 或 TMP；成员口径与全部判定缘由见同级 <c>TextComponent.md</c>。</summary>
    public class TextComponent : MonoBehaviour
    {
        #region 序列化字段
        /// <summary>字体 id（对应 FontCatalogSO 条目）；两种形态都生效，取不到则保持节点原有字体。</summary>
        [SerializeField] string fontId = "ui";

        /// <summary>字面量文案；无 languageKey 时显示它，同时是中文录入面（同步工具把它写进 SO 的 clips[0]）。</summary>
        [SerializeField, TextArea(3, 8)] string text = "";

        /// <summary>多语言 key（= LanguageClip.id）；有值时优先于字面量。</summary>
        [SerializeField] string languageKey = "";

        [SerializeField] int fontSize = 16;

        /// <summary>字重（Legacy <see cref="FontStyle"/> ↔ TMP <c>FontStyles</c>）。</summary>
        [SerializeField] FontStyle fontStyle = FontStyle.Normal;
        [SerializeField] Color color = Color.black;

        /// <summary>对齐（9 宫格 + Justified + Auto）；Auto = 不改节点原有对齐。</summary>
        [SerializeField] OverrideAlignment alignment = OverrideAlignment.Auto;

        /// <summary>逐节点开中文排版（NBSP 缩进 + 标点避头），默认关。</summary>
        [SerializeField] bool cjkTypography = false;
        #endregion

        #region 运行时状态
        /// <summary>当前生效的 Legacy 控件；与 <see cref="tmpText"/> 至多一个非空。</summary>
        Text legacyText;
        /// <summary>当前生效的 TMP 控件。</summary>
        TextMeshProUGUI tmpText;

        /// <summary>装配阶段：就绪门与补装的唯一判据。</summary>
        AssemblePhase assemblePhase = AssemblePhase.None;

        /// <summary>换向协程在跑；OnEnable 用它防重入。</summary>
        bool swapping;

        /// <summary>要换形态但当时起不了协程（未激活层级）：等 OnEnable 补跑。</summary>
        bool swapPending;

        /// <summary>换向失败的重试是否已用过（最多一次）。</summary>
        bool swapRetried;

        /// <summary>是否已订阅 <see cref="GlobalDataMgr.Ready"/>（退订在 OnDestroy）。</summary>
        bool readySubscribed;

        /// <summary>装配期间缓冲的写入种类（三态）。</summary>
        PendingKind pendingKind = PendingKind.None;
        /// <summary>缓冲的值（字面量或 key）。</summary>
        string pendingValue;
        /// <summary>缓冲的条目（<c>PendingKind.Clip</c> 时才有值）。</summary>
        LanguageClip pendingClip;

        /// <summary>外部显式写过文案 ⇒ 不再用 languageKey / 字面量覆盖它。</summary>
        bool externallyDriven;

        /// <summary>当前原文（不含排版标记）。</summary>
        string rawText = string.Empty;

        /// <summary>finished 状态机（已达层 / 帧预算 / 已注册回调）。</summary>
        readonly TextFinishTracker tracker = new TextFinishTracker();
        /// <summary>避头协程在跑（防重入）。</summary>
        bool avoiding;
        /// <summary>稳定链路协程的代次：失活会打断协程，重入时靠它让旧协程退出。</summary>
        int settleGeneration;
        #endregion

        #region 只读视图（Editor 工具与校验用）
        /// <summary>字体 id。</summary>
        public string FontId => fontId;
        /// <summary>多语言 key。</summary>
        public string LanguageKey => languageKey;
        /// <summary>是否开了中文排版。</summary>
        public bool CjkTypography => cjkTypography;
        /// <summary>当前原文（不含排版标记）。</summary>
        public string RawText => rawText;
        /// <summary>序列化字段 text（中文录入面）；运行时 <see cref="RawText"/> 在编辑期恒为空，故工具必须读这个。</summary>
        public string LiteralText => text;
        /// <summary>写入是否安全（装配完成）；**不等于**显示稳定，后者看 <see cref="OnFinished"/>。</summary>
        public bool Ready => assemblePhase == AssemblePhase.Ready;
        /// <summary>当前装配阶段。</summary>
        public AssemblePhase Phase => assemblePhase;
        /// <summary>字号（读写）。</summary>
        public int FontSizeValue { get => fontSize; set { if (fontSize == value) return; fontSize = value; ApplyStyle(); BeginFinishPipeline(); } }
        /// <summary>字重（读写）。</summary>
        public FontStyle FontStyleValue { get => fontStyle; set { if (fontStyle == value) return; fontStyle = value; ApplyStyle(); BeginFinishPipeline(); } }
        /// <summary>颜色（读写）。</summary>
        public Color ColorValue { get => color; set { if (color == value) return; color = value; ApplyStyle(); BeginFinishPipeline(); } }
        /// <summary>对齐（读写）；Auto = 不改。</summary>
        public OverrideAlignment Alignment { get => alignment; set { if (alignment == value) return; alignment = value; ApplyStyle(); BeginFinishPipeline(); } }
        #endregion

        #region 内部状态（供换向器使用）
        /// <summary>当前 Legacy 控件。</summary>
        internal Text LegacyTarget { get => legacyText; set => legacyText = value; }
        /// <summary>当前 TMP 控件。</summary>
        internal TextMeshProUGUI TmpTarget { get => tmpText; set => tmpText = value; }
        /// <summary>换向协程在跑。</summary>
        internal bool Swapping { get => swapping; set => swapping = value; }
        /// <summary>待换形态。</summary>
        internal bool SwapPending { get => swapPending; set => swapPending = value; }
        /// <summary>重试已用过。</summary>
        internal bool SwapRetried { get => swapRetried; set => swapRetried = value; }
        /// <summary>当前装配阶段。</summary>
        internal AssemblePhase CurrentPhase => assemblePhase;
        /// <summary>目标形态是否为 TMP。</summary>
        internal static bool WantsTmpForm() => WantsTmp();
        #endregion

        #region MonoBehaviour 生命周期
        /// <summary>就绪门：数据未就绪则只订阅 <see cref="GlobalDataMgr.Ready"/>，绝不用兜底值定型形态。</summary>
        void Awake()
        {
            if (GlobalDataMgr.IsReady)
            {
                Assemble();
                return;
            }

            GlobalDataMgr.Ready += OnDataReady;
            readySubscribed = true;
        }

        /// <summary>未激活层级起不了协程：补跑挂起的换向，以及挂起的 ③避头 / ④布局。</summary>
        void OnEnable()
        {
            if (assemblePhase == AssemblePhase.None)
            {
                if (GlobalDataMgr.IsReady) Assemble();
                return;
            }

            if (assemblePhase == AssemblePhase.Assembling)
            {
                if (swapping || !swapPending) return;
                // 形态正常不在运行期变；真变了就退回重新装配一次，不留"待换"这种半截状态。
                if (!WantsTmp()) { assemblePhase = AssemblePhase.None; Assemble(); return; }
                StartCoroutine(TextComponentSwapper.SwapToTmp(this));
                return;
            }

            if (!tracker.Running) return;

            if (tracker.Reached < TextFinishLayer.Typography && NeedsTypographyAvoidance() && !avoiding)
            {
                avoiding = true;
                StartCoroutine(AvoidLeadingPunctuation());
            }
            else if (tracker.Reached == TextFinishLayer.Typography && !tracker.LayoutRequested)
            {
                RequestFinishLayout();
                StartSettleRoutine();
            }
            else
            {
                StartSettleRoutine();
            }
        }

        /// <summary>编辑期补挂：两种文本控件都没有时给一个 TMP（编辑期默认 TMP），有任一控件则不动。</summary>
        void Reset()
        {
            if (GetComponent<TextMeshProUGUI>() != null || GetComponent<Text>() != null) return;
            gameObject.AddComponent<TextMeshProUGUI>();
        }

        /// <summary>退订就绪事件（静态事件不退订会回调到已销毁对象）并清空回调。</summary>
        void OnDestroy()
        {
            if (readySubscribed)
            {
                GlobalDataMgr.Ready -= OnDataReady;
                readySubscribed = false;
            }
            settleGeneration++;
            tracker.Clear();
        }
        #endregion

        #region 就绪门与装配
        /// <summary>就绪回调：退订 + 补装（幂等）。</summary>
        void OnDataReady()
        {
            if (readySubscribed)
            {
                GlobalDataMgr.Ready -= OnDataReady;
                readySubscribed = false;
            }
            if (assemblePhase == AssemblePhase.None) Assemble();
        }

        /// <summary>目标形态是否为 TMP（形态的唯一裁决者，冷启动读一次即定）。</summary>
        static bool WantsTmp() => GlobalDataMgr.GetTextType() == TextType.TMP;

        /// <summary>装配入口（幂等）：置 Assembling 后按目标形态分派。</summary>
        void Assemble()
        {
            if (assemblePhase != AssemblePhase.None) return;
            assemblePhase = AssemblePhase.Assembling;
            swapRetried = false;
            if (WantsTmp()) AssembleTmp();
            else AssembleLegacy();
        }

        /// <summary>目标形态 Legacy：已是 Legacy 就直接用；是 TMP 就换向（**不认领**节点既有控件）；都没有则补一个。</summary>
        void AssembleLegacy()
        {
            legacyText = GetComponent<Text>();
            if (legacyText != null)
            {
                tmpText = null;
                FinishAssemble();
                return;
            }

            tmpText = GetComponent<TextMeshProUGUI>();
            if (tmpText == null)
            {
                legacyText = gameObject.AddComponent<Text>();
                if (legacyText == null) { FailedImmediately(); return; }
                FinishAssemble();
                return;
            }

            if (isActiveAndEnabled) StartCoroutine(TextComponentSwapper.SwapToLegacy(this));
            else swapPending = true;
        }

        /// <summary>目标形态 TMP：已有 TMP 就直接用；只有 Legacy 时换向（Dropdown 文本节点豁免）；都没有则直接挂 TMP。</summary>
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
                if (tmpText == null) { FailedImmediately(); return; }
                FinishAssemble();
                return;
            }

            if (IsDropdownTextNode(legacyText))
            {
                Log.Verbose($"[TextComponent] 「{name}」是 Dropdown 的 caption/item 文本节点，保持 Legacy 形态");
                tmpText = null;
                FinishAssemble();
                return;
            }

            if (isActiveAndEnabled) StartCoroutine(TextComponentSwapper.SwapToTmp(this));
            else swapPending = true;
        }

        /// <summary>换向协程收尾（内部入口）：置 Ready → 下发样式 → 消费装配期间的写入。</summary>
        internal void CompleteSwap()
        {
            swapping = false;
            FinishAssemble();
        }

        /// <summary>阶段跃迁（内部入口，供换向器把失败态钉住）。</summary>
        internal void EnterPhase(AssemblePhase phase)
        {
            assemblePhase = phase;
        }

        /// <summary>没有换向过程就失败了（AddComponent 直接被拒）：下帧按同一方向重试一次，再失败即 Failed。</summary>
        void FailedImmediately()
        {
            swapping = false;
            if (!swapRetried)
            {
                swapRetried = true;
                swapPending = true;
                if (isActiveAndEnabled)
                {
                    if (WantsTmp()) StartCoroutine(TextComponentSwapper.SwapToTmp(this));
                    else StartCoroutine(TextComponentSwapper.SwapToLegacy(this));
                }
                return;
            }

            assemblePhase = AssemblePhase.Failed;
            swapPending = false;
            Log.Error($"[TextComponent] 「{name}」两次均未能挂上文本控件，节点暂无文本（后续写入继续缓冲在 pending）", this);
        }

        /// <summary>装配收尾（各路径共用）：置 Ready → 下发样式 → 消费装配期间到达的写入。</summary>
        void FinishAssemble()
        {
            assemblePhase = AssemblePhase.Ready;
            swapPending = false;
            swapping = false;
            ApplyStyle();
            ApplyPendingOrConfigured();
        }

        /// <summary>消费装配期间的缓冲写入；无缓冲则按配置取文案（外部驱动过就补写它那个字面量）。</summary>
        void ApplyPendingOrConfigured()
        {
            if (pendingKind != PendingKind.None)
            {
                PendingKind kind = pendingKind;
                string value = pendingValue;
                LanguageClip clip = pendingClip;
                pendingKind = PendingKind.None;
                pendingValue = null;
                pendingClip = null;

                switch (kind)
                {
                    case PendingKind.Literal: SetText(value); return;
                    case PendingKind.Key: SetTextKey(value); return;
                    case PendingKind.Clip: SetText(clip); return;
                }
                return;
            }

            if (!externallyDriven) ApplyConfiguredText();
            else ApplyRawText(text);
        }

        /// <summary>该 Legacy 节点是否是某个 Dropdown 的 caption / item 文本（是则不能换 TMP，两个方向都豁免）。</summary>
        static bool IsDropdownTextNode(Text node)
        {
            if (node == null) return false;
            var owners = node.GetComponentsInParent<Dropdown>(true);
            for (int i = 0; i < owners.Length; i++)
                if (owners[i] != null && (owners[i].captionText == node || owners[i].itemText == node)) return true;
            return false;
        }
        #endregion

        #region 对外 API（唯一写入路径）
        /// <summary>写入字面量文案（运行期数据走这里）；装配未完成则先缓冲。</summary>
        public void SetText(string value)
        {
            if (!Ready)
            {
                pendingKind = PendingKind.Literal;
                pendingValue = value;
                pendingClip = null;
                return;
            }
            externallyDriven = true;
            text = value ?? string.Empty;
            ApplyRawText(text);
        }

        /// <summary>写入多语言 key（静态文案走这里）；装配未完成则先缓冲。</summary>
        public void SetTextKey(string key)
        {
            if (!Ready)
            {
                pendingKind = PendingKind.Key;
                pendingValue = key;
                pendingClip = null;
                return;
            }
            externallyDriven = false;
            languageKey = key ?? string.Empty;
            ApplyConfiguredText();
        }

        /// <summary>写入一条 <see cref="LanguageClip"/>（不查 key 表，等价于把 languageKey 置成 clip.id）；clip 为 null 则载入字面量。</summary>
        public void SetText(LanguageClip clip)
        {
            if (!Ready)
            {
                pendingKind = PendingKind.Clip;
                pendingClip = clip;
                pendingValue = null;
                return;
            }

            if (clip == null)
            {
                SetText(text);
                return;
            }

            externallyDriven = false;
            languageKey = clip.id;
            ApplyConfiguredText();
        }

        /// <summary>按 key / 字面量重取文案；装配未完成或外部驱动过时直接返回。</summary>
        public void Refresh()
        {
            if (!Ready) return;
            if (externallyDriven) return;
            ApplyConfiguredText();
        }

        /// <summary>当前显示用的原文（不含排版标记）。</summary>
        public string TextValue => rawText;

        /// <summary>注册"稳定后回调"；已达成则同帧立即回调，未达成则挂起等下次达成（重复注册不去重）。</summary>
        public void OnFinished(Action callback, TextFinishLayer layer = TextFinishLayer.Layout)
        {
            tracker.Add(layer, callback, InvokeSafely);
        }
        #endregion

        #region 稳定链路（②之后：③排版 → ④布局 → 回调）
        /// <summary>帧上限：避头不收敛时到限即按已稳定放行并告警，绝不让消费方永远等下去。</summary>
        public const int FinishFrameLimit = 10;

        /// <summary>② 赋值 / 样式变更后启动稳定链路：复位已达层与帧预算，再推进 ③排版 → ④布局。</summary>
        void BeginFinishPipeline()
        {
            tracker.Begin();

            if (NeedsTypographyAvoidance() && isActiveAndEnabled)
            {
                if (!avoiding)
                {
                    avoiding = true;
                    StartCoroutine(AvoidLeadingPunctuation());
                }
                return;
            }

            SettleTypography();
        }

        /// <summary>③ 收敛（避头跑完 / 无需避头）：置 Typography 达成，推进 ④ 并起稳定协程。</summary>
        void SettleTypography()
        {
            avoiding = false;
            tracker.MarkTypography();
            RequestFinishLayout();
            StartSettleRoutine();
        }

        /// <summary>④ 布局：向所属面板请求重建（不直连 UILayoutRebuilder），下一帧末确认；无面板祖先则跳过并同帧达成。</summary>
        void RequestFinishLayout()
        {
            PanelBase panel = GetComponentInParent<PanelBase>(true);
            if (panel == null)
            {
                Log.Verbose($"[TextComponent] 「{name}」不在任何面板下，跳过 ④ 布局重建（Layout 与 Typography 同帧达成）");
                tracker.SkipLayout();
                return;
            }

            if (!isActiveAndEnabled) return;

            panel.RequestLayoutRebuild(transform);
            tracker.NoteLayoutRequested(Time.frameCount);
        }

        /// <summary>起稳定协程（代次自增，旧协程随即退出）；未激活起不了协程，交由 OnEnable 续。</summary>
        void StartSettleRoutine()
        {
            if (!isActiveAndEnabled) return;
            settleGeneration++;
            StartCoroutine(SettleRoutine(settleGeneration));
        }

        /// <summary>逐帧推进稳定链路：到帧上限即放行，全部达成即收尾（不用 Update 周期）。</summary>
        IEnumerator SettleRoutine(int generation)
        {
            while (generation == settleGeneration && tracker.Running)
            {
                if (tracker.Frames >= FinishFrameLimit)
                {
                    if (tracker.Reached < TextFinishLayer.Layout)
                    {
                        Log.Warning($"[TextComponent] 文本「{name}」在 {FinishFrameLimit} 帧内未收敛（排版避头不收敛？），按已稳定放行", this);
                        tracker.ForceSettle();
                    }
                    break;
                }

                if (tracker.Advance(Time.frameCount)) break;
                yield return null;
            }

            if (generation != settleGeneration) yield break;
            tracker.Complete(InvokeSafely);
        }

        /// <summary>回调异常隔离：一个回调抛异常不中断其余回调，也不打断组件自身。</summary>
        void InvokeSafely(Action callback)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"[TextComponent] OnFinished 回调抛异常（{name}）：{e.Message}", this);
            }
        }

        /// <summary>是否需要等 ③ 避头收敛（避头仅 Legacy 有，TMP 没有等价物）。</summary>
        bool NeedsTypographyAvoidance() => cjkTypography && legacyText != null;

        /// <summary>标点避头（Legacy 专用）：每轮等一帧重排、只挪一个行首标点，收敛后置 Typography 达成。</summary>
        IEnumerator AvoidLeadingPunctuation()
        {
            yield return null;

            while (legacyText != null)
            {
                TextGenerator generator = legacyText.cachedTextGenerator;
                if (generator == null || generator.lineCount <= 1) break;

                var sb = new StringBuilder(legacyText.text);
                var lines = generator.lines;
                bool inserted = false;
                for (int i = 1; i < lines.Count; i++)
                {
                    int index = lines[i].startCharIdx;
                    if (index <= 0 || index >= sb.Length) continue;
                    if (sb[index - 1] == '\n') continue;
                    if (!TextTypography.IsLeadingPunctuation(sb[index])) continue;
                    sb.Insert(index - 1, '\n');
                    inserted = true;
                    break;
                }
                if (!inserted) break;

                legacyText.text = sb.ToString();
                yield return null;
            }

            SettleTypography();
        }
        #endregion

        #region 静态过渡入口（面板持有的是 Text/TMP 引用时用）
        /// <summary>写 Legacy 文本节点；节点上有组件就走组件（享受缓冲 / 排版 / 多语言），没有则直写。</summary>
        public static void SetTextOn(Text target, string value)
        {
            if (target == null) return;
            if (target.TryGetComponent(out TextComponent comp)) { comp.SetText(value); return; }
            target.text = value;
        }

        /// <summary>写 TMP 文本节点；节点上有组件就走组件，没有则直写。</summary>
        public static void SetTextOn(TextMeshProUGUI target, string value)
        {
            if (target == null) return;
            if (target.TryGetComponent(out TextComponent comp)) { comp.SetText(value); return; }
            target.text = value;
        }

        /// <summary>写颜色：有组件走组件（换向卸载后旧引用是"假 null"，直写要么 no-op 要么抛），没有则直写。</summary>
        public static void SetColorOn(Text target, Color color)
        {
            if (target == null) return;
            if (target.TryGetComponent(out TextComponent comp)) { comp.ColorValue = color; return; }
            target.color = color;
        }

        /// <summary>读节点原文（有组件时返回 RawText，避免拿到 NBSP 缩进等排版标记）。</summary>
        public static string ReadRaw(Text target)
        {
            if (target == null) return string.Empty;
            return target.TryGetComponent(out TextComponent comp) ? comp.RawText : target.text;
        }

        /// <summary>读 TMP 节点原文。</summary>
        public static string ReadRaw(TextMeshProUGUI target)
        {
            if (target == null) return string.Empty;
            return target.TryGetComponent(out TextComponent comp) ? comp.RawText : target.text;
        }

        /// <summary>取文本节点：组件优先（换向后旧字段是"假 null"，用它取 transform / parent 会静默失效），都没有则 null。</summary>
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

        /// <summary>把本组件写过的排版还原成原文（实现见 <see cref="TextTypography"/>）。</summary>
        public static string Remove(string s) => TextTypography.Remove(s);

        /// <summary>去掉所有换行符（实现见 <see cref="TextTypography"/>）。</summary>
        public static string RemoveNewlines(string s) => TextTypography.RemoveNewlines(s);
        #endregion

        #region 取文案与样式下发
        /// <summary>按 languageKey 取文案（回退链只有一份实现：GlobalDataMgr.PickClipText）；无 key 用字面量。</summary>
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

        /// <summary>把原文（按需排版）写进当前控件，并启动稳定链路。</summary>
        void ApplyRawText(string value)
        {
            rawText = value ?? string.Empty;
            WriteToTarget(cjkTypography ? TextTypography.BuildDisplay(rawText) : rawText);
            BeginFinishPipeline();
        }

        /// <summary>把显示文写进当前控件（Legacy 与 TMP 分别写）。</summary>
        void WriteToTarget(string display)
        {
            if (legacyText != null) legacyText.text = display;
            else if (tmpText != null) tmpText.text = display;
        }

        /// <summary>下发样式：字体 → 字号 → 颜色 → 字重 → 对齐（先换字体，字重才落在正确字体上）。</summary>
        void ApplyStyle()
        {
            if (legacyText != null)
            {
                Font font = GlobalAssetsMgr.GetFontByFontId(fontId);
                if (font != null) legacyText.font = font;
                legacyText.fontSize = fontSize;
                legacyText.color = color;
                legacyText.fontStyle = fontStyle;
                TextAlignmentMap.ApplyTo(legacyText, alignment);
            }
            else if (tmpText != null)
            {
                TMP_FontAsset font = GlobalAssetsMgr.GetTmpFontAssetByFontId(fontId);
                if (font != null) tmpText.font = font;
                tmpText.fontSize = fontSize;
                tmpText.color = color;
                tmpText.fontStyle = TextAlignmentMap.ToTmpFontStyle(fontStyle);
                TextAlignmentMap.ApplyTo(tmpText, alignment);
            }
        }
        #endregion
    }
}
