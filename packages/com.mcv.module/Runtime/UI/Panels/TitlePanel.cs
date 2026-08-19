using System.Collections;
using MCV_Module.Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class TitlePanel : PanelBase
    {
        [SerializeField] Text titleText;
        [SerializeField] Text engTitleText;
        [SerializeField] Image icoImage;
        HorizontalLayoutGroup m_LayoutGroup;
        RectTransform m_LayoutRect;
        readonly static Vector2 offsetLimit = new Vector2(0, -250f);
        bool isActiveNow = true;
        bool m_TargetActive = true;   // 当前动画/静止所朝向的目标状态，用于防重复触发

        protected override void Awake()
        {
            base.Awake();

            if (titleText == null || icoImage == null)
            {
                Debug.LogError($"[TitlePanel] 缺少必要组件", this);
                return;
            }
            m_LayoutGroup = GetComponent<HorizontalLayoutGroup>();
            m_LayoutRect = m_LayoutGroup != null ? (RectTransform)m_LayoutGroup.transform : null;
        }

        protected override void Start()
        {
            base.Start();
            StartCoroutine(DelayStart());

            var rect = GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            m_TargetActive = isActiveNow;
            ActiveState(isActiveNow);
        }

        // 测试方法
        // void Update()
        // {
        //     if (Keyboard.current.escapeKey.wasPressedThisFrame)
        //     {
        //         SetUIActive(!isActiveNow);
        //         isActiveNow = !isActiveNow;
        //     }
        // }

        IEnumerator DelayStart()
        {
            while (!GlobalDataMgr.Exists || !GlobalDataMgr.Instance.IsInit)
            {
                yield return null;
            }

            yield return null;

            SetTitle(GlobalDataMgr.Instance.SystemData.projectInfo.projectName, GlobalDataMgr.Instance.SystemData.projectInfo.projectEnglishName);
        }

        public void SetTitle(string title, string engTitle)
        {
            titleText.text = title;
            engTitleText.text = engTitle;

            var rect = transform.parent.GetComponent<RectTransform>();
            var child = titleText.GetComponent<RectTransform>();
            var parent = child.parent.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(child);
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        #region 覆盖Active方法
        public override void SetUIActive(bool isActive)
        {
            // 已是目标状态（静止或正在动画前往），不重复触发，避免 switch alpha 出现 0-1-0 抖动
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
                // 与协程一致：动画的是 padding.left，不是 spacing
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
                // padding 改动后必须强制重建布局，否则子物体位置不会逐帧更新（表现为瞬间跳到终点）
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