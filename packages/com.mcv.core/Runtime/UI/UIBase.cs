using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MCV_Module.UI
{
    [RequireComponent(typeof(CanvasGroup))]
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
        /// <summary>
        /// 设置UI显示
        /// </summary>
        /// <param name="isActive"></param>
        public virtual void SetUIActive(bool isActive)
        {
            SetUIActive(isActive, null);
        }

        /// <summary>
        /// 设置 UI 显示，并可在「收起动画播放完成」后执行回调。
        /// 用于"先播完收起动画再通知业务方"，避免面板提前失活导致 StartCoroutine 报错。
        /// </summary>
        /// <param name="isActive">是否显示。</param>
        /// <param name="onHidden">isActive=false 且收起动画完成后回调（面板 SetActive(false) 之前）。</param>
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
        /// <summary>
        /// 设置UI显示并立即
        /// </summary>
        /// <param name="isActive"></param>
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

        /// <summary>
        /// 停止当前进行中的显示动画，并复位相关状态。
        /// 判空处理：协程可能被外部 Stop 掉导致 ActiveAnimCoroutine 为 null，避免 StopCoroutine(null) 抛 "routine is null"。
        /// </summary>
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
