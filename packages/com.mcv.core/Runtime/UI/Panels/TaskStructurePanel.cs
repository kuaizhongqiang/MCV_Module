// 由 MCV Editor/创建/UI Panel 生成器生成（2026-09-14）—— 请按需补充业务代码
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    /// <summary>结构页面板（View）—— 跟随鼠标的浮动提示框（TipsFloat），开关与文案都由 TaskStructureController 决定。</summary>
    [RequireController(typeof(MCV_Module.Controllers.TaskStructureController))]
    public class TaskStructurePanel : TaskPanelBase
    {
        [Tooltip("浮动框根（跟着鼠标走的那个节点，通常是 TipsFloat）")]
        [SerializeField] RectTransform floatParent;
        [Tooltip("浮动框里的文字（TipsFloat/ContentText 上的 Text）")]
        [SerializeField] Text tipsText;
        /// <summary>提示文本节点上的组件（TMP 形态下本组件认领的控件是 TMP、Legacy 已被卸载，写入必须经它）。</summary>
        TextComponent m_TipsTextComp;
        [Tooltip("浮动框相对鼠标的偏移（Canvas 单位）")]
        [SerializeField] Vector2 tipsOffset = new Vector2(0f, 80f);

        /// <summary>浮动框是否正在跟随鼠标</summary>
        bool isTipsShow;

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            // WHY: 必须此刻解析并缓存 —— TMP 形态下该节点上的 Legacy Text 会被卸载，而 Destroy 到帧末才生效，
            // 所以本帧内 GetComponent 仍稳定可用；之后 tipsText 成"假 null"、静态入口会静默 no-op。
            if (tipsText != null) m_TipsTextComp = tipsText.GetComponent<TextComponent>();
            CloseTips();      // 打开面板先收起浮动框，等第一次移入结构零件再开
        }

        public override string GetPanelContent()
        {
            string result = "";

            return result;
        }
        #endregion

        #region 浮动提示框（由控制器驱动）
        /// <summary>打开浮动框并写入文案（移入 StructureTaskObj 时由控制器调用）。</summary>
        public void ShowTips(string text)
        {
            if (floatParent == null || (tipsText == null && m_TipsTextComp == null))
            {
                Log.Warning($"[{GetType().Name}] 浮动框未配置（floatParent / tipsText），结构名「{text}」无法显示");
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

        /// <summary>关闭浮动框（移出结构零件 / 收口时由控制器调用）。</summary>
        public void CloseTips()
        {
            isTipsShow = false;
            if (floatParent != null) floatParent.gameObject.SetActive(false);
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
