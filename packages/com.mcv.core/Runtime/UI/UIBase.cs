using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MCV_Module.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    /// <summary>UI 元素基类：经 CanvasGroup 做显隐淡入淡出，并支持「收起动画播完再回调」。</summary>
    public abstract class UIBase : MonoBehaviour
    {
        protected CanvasGroup canvasGroup;
        protected Coroutine ActiveAnimCoroutine;
        protected bool isAnimating = false;
        /// <summary>收起动画完成后的回调（用于"先播完收起动画再通知业务方"，避免面板提前失活导致协程报错）。</summary>
        Action m_OnHiddenCallback;
        [Header("初始状态"),Tooltip("是否在实例化时显示")]
        [SerializeField] bool isActiveOnInstance = true;
        [Header("交互状态"), Tooltip("是否可交互")]
        [SerializeField] bool isInteractable = true;
        [Header("动画时间"), Tooltip("显示动画时间")]
        [SerializeField] protected float animTime = 0.3f;

        /// <summary>动画时长（供外部估算等待时间）。</summary>
        public float AnimDuration => animTime;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.interactable = isInteractable;
            canvasGroup.blocksRaycasts = isInteractable;
            gameObject.SetActive(isActiveOnInstance);
        }

        protected virtual void OnDestroy()
        {
            
        }

        #region 显示控制
        /// <summary>设置 UI 显示（带淡入淡出动画）。</summary>
        public virtual void SetUIActive(bool isActive)
        {
            SetUIActive(isActive, null);
        }

        /// <summary>设置 UI 显示，可在「收起动画播完」后回调（在面板 SetActive(false) 之前执行）。</summary>
        public virtual void SetUIActive(bool isActive, Action onHidden)
        {
            StopRunningAnim();

            if (isActive)
            {
                m_OnHiddenCallback = null;   // 显示时不期望触发隐藏回调，清掉避免残留
            }
            else
            {
                m_OnHiddenCallback = onHidden;
            }

            if (isActive)
            {
                canvasGroup.alpha = 0;
                gameObject.SetActive(true);
            }

            ActiveAnimCoroutine = StartCoroutine(Anim(isActive));
        }
        /// <summary>立即设置 UI 显示（无动画）。</summary>
        public virtual void SetUIActiveImmediately(bool isActive)
        {
            StopRunningAnim();

            gameObject.SetActive(isActive);
            canvasGroup.alpha = isActive ? 1 : 0;

            if (isInteractable)
            {
                canvasGroup.interactable = isActive;
                canvasGroup.blocksRaycasts = isActive;
            }
        }

        /// <summary>停止当前显示动画并复位状态（判空：协程可能被外部 Stop 掉，StopCoroutine(null) 会抛异常）。</summary>
        void StopRunningAnim()
        {
            if (isAnimating)
            {
                if (ActiveAnimCoroutine != null)
                {
                    StopCoroutine(ActiveAnimCoroutine);
                }
                ActiveAnimCoroutine = null;
                isAnimating = false;
            }
        }
        #endregion

        #region 显示动画
        IEnumerator Anim(bool isActive)
        {
            float time = 0;
            float currentAlpha = canvasGroup.alpha;
            float targetAlpha = isActive ? 1 : 0;
            isAnimating = true;

            while (time < animTime)
            {
                time += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, time / animTime);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            if (isInteractable)
            {
                canvasGroup.interactable = isActive;
                canvasGroup.blocksRaycasts = isActive;
            }
            isAnimating = false;
            ActiveAnimCoroutine = null;

            if (!isActive)
            {
                // 先通知"收起动画已完成"，再失活面板（保证回调在面板仍 active 时执行，避免协程报错）
                var cb = m_OnHiddenCallback;
                m_OnHiddenCallback = null;
                cb?.Invoke();
                gameObject.SetActive(false);
            }
        }
        #endregion

        #region 工具方法
        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
        #endregion
    }
}
