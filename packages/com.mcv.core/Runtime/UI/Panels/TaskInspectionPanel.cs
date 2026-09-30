// 由 MCV Editor/创建/UI Panel 生成器生成（2026-09-14）—— 请按需补充业务代码

using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    /// <summary>检测（测量）页面板（View）—— 跟随鼠标的浮动提示框 + 固定操作记录，开关与文案都由 TaskInspectionController 决定。</summary>
    [RequireController(typeof(MCV_Module.Controllers.TaskInspectionController))]
    public class TaskInspectionPanel : TaskPanelBase
    {
        [Tooltip("浮动框根（跟着鼠标走的那个节点，通常是 TipsFloat）")]
        [SerializeField] RectTransform floatParent;
        [Tooltip("浮动框里的文字（TipsFloat/ContentText 上的 Text）")]
        [SerializeField] Text tipsText;
        [Tooltip("浮动框相对鼠标的偏移（Canvas 单位）")]
        [SerializeField] Vector2 tipsOffset = new Vector2(0f, 80f);

        [Tooltip("操作记录文本（固定位置，不跟随鼠标；只显示最新一条，不是逐条累加）")]
        [SerializeField] Text opRecordText;
        [Tooltip("还没有任何接触时显示的占位文字（不要留空：空文本会让文本框的背景/排版塌成一条，样式很怪）")]
        [SerializeField] string opRecordDefaultText = "暂无操作记录";

        // WHY: 两个文本节点都要提前缓存组件 —— TMP 形态下组件会卸载节点上的 Legacy Text，tipsText / opRecordText 随后成"假 null"，
        // 静态入口 SetTextOn 会静默 no-op、ReadRaw 读回空串，只有持有组件才读写得到。
        /// <summary>浮动提示框文本节点上的组件（缓存见上）。</summary>
        TextComponent m_TipsTextComp;
        /// <summary>操作记录文本节点上的组件（缓存见上）。</summary>
        TextComponent m_OpRecordTextComp;

        /// <summary>浮动框是否正在跟随鼠标</summary>
        bool isTipsShow;

        #region 生命周期
        protected override void Awake()
        {
            // WHY: 先走基类——配置缺失也要完成面板初始化，把 return 写在 base 前面会跳过绑定流程
            base.Awake();

            // WHY: 在调用之前解析一次（此刻节点上的 Legacy Text 还在，GetComponent 稳定可用）—— 换过形态后字段成"假 null"，
            // 那时再解析会抛，静态写入也会被静默丢弃。
            if (tipsText != null) m_TipsTextComp = tipsText.GetComponent<TextComponent>();
            if (opRecordText != null) m_OpRecordTextComp = opRecordText.GetComponent<TextComponent>();

            CloseTips();          // 打开面板先收起浮动框，等第一次接触再开
            ClearOpRecord();
        }

        public override string GetPanelContent()
        {
            return string.Empty;
        }
        #endregion

        #region 浮动提示框（由控制器驱动）
        /// <summary>打开浮动框并写入文案（表笔接触检测点时由控制器调用）。</summary>
        public void ShowTips(string text)
        {
            // WHY: 判空连组件一起看 —— 换过形态后 tipsText 成"假 null"，只看字段会把"能显示"误判成"未配置"，整条提示都不显示。
            if (floatParent == null || (tipsText == null && m_TipsTextComp == null))
            {
                Log.Warning($"[{GetType().Name}] 浮动框未配置（floatParent / tipsText），提示「{text}」无法显示");
                return;
            }

            // WHY: 先激活再写文本 —— TMP 形态下"换组件"的 OnEnable 是在激活那一刻才跑的，激活前写只会进 pending 缓冲
            floatParent.gameObject.SetActive(true);
            isTipsShow = true;

            if (m_TipsTextComp != null) m_TipsTextComp.SetText(text ?? string.Empty);
            else TextComponent.SetTextOn(tipsText, text ?? string.Empty);

            // WHY: 不能同帧直接 ForceRebuild —— 此刻 TMP 可能还没挂上（Legacy 已被禁用卸载），量到的是空文本的尺寸；
            //      且浮动框是「父级 LayoutGroup + 子节点自带 ContentSizeFitter」，必须等一帧后自下而上刷（见 UILayoutRebuilder）
            RequestLayoutRebuild(floatParent);

            FollowMouse();    // 同帧先摆到位，避免第一帧闪在上一处位置
        }

        /// <summary>关闭浮动框（表笔脱离接触 / 收口时由控制器调用）。</summary>
        public void CloseTips()
        {
            isTipsShow = false;
            if (floatParent != null) floatParent.gameObject.SetActive(false);
        }
        #endregion

        #region 操作记录（由控制器驱动）
        // WHY: 文案与当前相同时直接跳过——表笔检测失败会回弹到原位，记录本就该保持不变
        /// <summary>更新操作记录（只显示最新一条，覆盖式、不逐条累加）。</summary>
        public void SetOpRecord(string line)
        {
            if (opRecordText == null && m_OpRecordTextComp == null)
            {
                Log.Warning($"[{GetType().Name}] opRecordText 未配置，操作记录「{line}」无法显示");
                return;
            }

            if (string.IsNullOrEmpty(line)) return;     // 空文案不覆盖，否则占位文字会被擦掉、样式塌掉
            if (ReadOpRecord() == line) return;

            if (m_OpRecordTextComp != null) m_OpRecordTextComp.SetText(line);
            else TextComponent.SetTextOn(opRecordText, line);
            RebuildOpRecordLayout();
        }

        // WHY: 复位要写回占位文字而不是空串——空文本会让文本框背景/排版塌成一条，面板刚打开时样式很怪
        /// <summary>复位操作记录为占位文字（面板重建时由控制器调用）。</summary>
        public void ClearOpRecord()
        {
            if (opRecordText == null && m_OpRecordTextComp == null) return;

            string text = opRecordDefaultText ?? string.Empty;
            if (ReadOpRecord() == text) return;

            if (m_OpRecordTextComp != null) m_OpRecordTextComp.SetText(text);
            else TextComponent.SetTextOn(opRecordText, text);
            RebuildOpRecordLayout();
        }

        // WHY: 换过形态后 Legacy 字段读不到原文，必须优先走组件；两者都没有才算真的没有。
        /// <summary>读回操作记录当前原文（有组件走组件，避免拿到排版标记或空串）。</summary>
        string ReadOpRecord()
        {
            if (m_OpRecordTextComp != null) return m_OpRecordTextComp.RawText;
            return opRecordText != null ? TextComponent.ReadRaw(opRecordText) : string.Empty;
        }

        /// <summary>记录文本换了就要刷一次布局：文本框背景按内容自适应，不刷会停在旧尺寸。</summary>
        void RebuildOpRecordLayout()
        {
            // WHY: 取节点一律走组件优先（TMP 形态下 opRecordText 是"假 null"，对已卸载引用取 transform 会抛/静默失效）。
            Transform node = TextComponent.NodeOf(opRecordText, m_OpRecordTextComp);
            if (node == null) return;      // 节点解析不出来就跳过，别把空引用丢给重建

            // WHY: 刷的是记录框那一层（它的 LayoutGroup + 子节点 fitter），等一帧 + 自下而上交给统一入口
            RequestLayoutRebuild(node.parent != null ? node.parent : node);
        }
        #endregion

        #region 跟随鼠标
        void Update()
        {
            if (isTipsShow) FollowMouse();
        }

        // WHY: Canvas 是 Scale With Screen Size + 1920×1080，屏幕像素 ≠ Canvas 单位，且只能用 localPosition（anchoredPosition 相对锚点、改锚点即错位）
        /// <summary>把鼠标屏幕坐标换算到浮动框父节点的本地坐标后贴鼠标摆位。</summary>
        void FollowMouse()
        {
            if (floatParent == null) return;

            var parentRect = floatParent.parent as RectTransform;
            if (parentRect == null)
            {
                Log.Warning($"[{GetType().Name}] 浮动框的父节点不是 RectTransform，无法跟随鼠标");
                return;
            }

            Canvas canvas = floatParent.GetComponentInParent<Canvas>();
            Vector2 screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, screenPos, canvas != null ? canvas.worldCamera : null, out Vector2 localPoint))
                return;

            floatParent.localPosition = localPoint + tipsOffset;
        }
        #endregion
    }
}
