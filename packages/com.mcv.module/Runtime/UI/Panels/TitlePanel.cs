using System.Collections;
using MCV_Module.Utils;
using MCV_Module.Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Panels
{
    public class TitlePanel : PanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Text engTitleText;
        [SerializeField] Image icoImage;

        // WHY: 两个文本节点上的组件要提前缓存 —— TMP 形态下组件会卸载节点上的 Legacy Text，字段随后成"假 null"，
        // 静态入口 SetTextOn 会静默 no-op，GetComponent<RectTransform>() 也会抛。
        /// <summary>中文标题节点上的组件（缓存见上）。</summary>
        TextComponent m_TitleTextComp;
        /// <summary>英文标题节点上的组件（缓存见上）。</summary>
        TextComponent m_EngTitleTextComp;

        HorizontalLayoutGroup m_LayoutGroup;
        RectTransform m_LayoutRect;
        readonly static Vector2 offsetLimit = new Vector2(0, -250f);
        bool isActiveNow = true;
        bool m_TargetActive = true;   // WHY: 当前动画/静止所朝向的目标状态，用于防重复触发

        protected override void Awake()
        {
            base.Awake();

            // WHY: 在判空 return 之前解析一次（此时节点上的 Legacy Text 还在，GetComponent 稳定可用）—— 换形态后字段成
            // "假 null"，SetTitle（DelayStart 里会调）里的 GetComponent<RectTransform>() 会抛、文本写入会被静默丢弃。
            if (titleText != null) m_TitleTextComp = titleText.GetComponent<TextComponent>();
            if (engTitleText != null) m_EngTitleTextComp = engTitleText.GetComponent<TextComponent>();

            // WHY: 组件存在即视为已配置 —— 换形态后 Legacy 被卸载、titleText 变成 null 是正常状态，只有字段与缓存组件都为 null 才算缺配置。
            if ((titleText == null && m_TitleTextComp == null) || icoImage == null)
            {
                Log.Error($"[TitlePanel] 缺少必要组件", this);
                return;
            }
            m_LayoutGroup = GetComponent<HorizontalLayoutGroup>();
            m_LayoutRect = m_LayoutGroup != null ? (RectTransform)m_LayoutGroup.transform : null;
        }

        protected override void Start()
        {
            base.Start();
            StartCoroutine(DelayStart());

            RequestLayoutRebuild();

            m_TargetActive = isActiveNow;
            ActiveState(isActiveNow);
        }

        IEnumerator DelayStart()
        {
            while (!GlobalDataMgr.Exists || !GlobalDataMgr.Instance.IsInit)
            {
                yield return null;
            }

            yield return null;

            SetTitle(Localized.Pick(GlobalDataMgr.Instance.SystemData.projectInfo.projectName, GlobalDataMgr.Instance.SystemData.projectInfo.projectNameEn), GlobalDataMgr.Instance.SystemData.projectInfo.projectEnglishName);
        }

        public void SetTitle(string title, string engTitle)
        {
            if (m_TitleTextComp != null) m_TitleTextComp.SetText(title);
            else TextComponent.SetTextOn(titleText, title);
            if (m_EngTitleTextComp != null) m_EngTitleTextComp.SetText(engTitle);
            else TextComponent.SetTextOn(engTitleText, engTitle);

            // WHY: 标题/英文标题都挂在面板的 LayoutGroup 下，改完文本交给统一入口（等一帧 + 自下而上 + 防重入）——
            //      同帧直接 ForceRebuild 会量到 TMP 形态下还没装配完的空文本（表现为"第一次进来标题排版不对"）。
            RequestLayoutRebuild();
        }

        #region 覆盖Active方法
        public override void SetUIActive(bool isActive)
        {
            // WHY: 已是目标状态（静止或正在动画前往），不重复触发，避免 switch alpha 出现 0-1-0 抖动
            if (isActive == m_TargetActive) return;

            m_TargetActive = isActive;
            if (ActiveAnimCoroutine != null)
            {
                StopCoroutine(ActiveAnimCoroutine);
            }
            ActiveAnimCoroutine = StartCoroutine(OverrideAnimCoroutine(isActive));
        }

        public override void SetUIActiveImmediately(bool isActive)
        {
            m_TargetActive = isActive;
            if (ActiveAnimCoroutine != null)
            {
                StopCoroutine(ActiveAnimCoroutine);
            }

            ActiveState(isActive);
        }

        void ActiveState(bool isActive)
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = isActive;
                canvasGroup.blocksRaycasts = isActive;
                canvasGroup.alpha = isActive ? 1 : 0;
            }

            if (m_LayoutGroup != null)
            {
                // WHY: 与协程一致，动画的是 padding.left 而不是 spacing
                m_LayoutGroup.padding.left = (int)(isActive ? offsetLimit.x : offsetLimit.y);
                if (m_LayoutRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_LayoutRect);
                }
            }
        }

        IEnumerator OverrideAnimCoroutine(bool isActive)
        {
            isAnimating = true;
            float time = 0f;
            float currentLayoutAlpha = canvasGroup != null ? canvasGroup.alpha : (isActive ? 0 : 1);
            float targetLayoutAlpha = isActive ? 1 : 0;
            int currentSpacing = m_LayoutGroup.padding.left;
            int targetOffset = isActive? (int) offsetLimit.x : (int)offsetLimit.y;

            while (time < animTime)
            {
                time += Time.deltaTime;
                float t = time / animTime;
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(currentLayoutAlpha, targetLayoutAlpha, t);
                m_LayoutGroup.padding.left = (int)Mathf.Lerp(currentSpacing, targetOffset, t);
                // WHY: padding 改动后必须强制重建布局，否则子物体位置不会逐帧更新（表现为瞬间跳到终点）
                if (m_LayoutRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(m_LayoutRect);
                }
                yield return null;
            }

            ActiveState(isActive);

            ActiveAnimCoroutine = null;
            isAnimating = false;
        }
        #endregion
    }
}