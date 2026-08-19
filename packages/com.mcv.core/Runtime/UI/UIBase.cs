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
        [Header("初始状态"),Tooltip("是否在实例化时显示")]
        [SerializeField] bool isActiveOnInstance = true;
        [Header("交互状态"), Tooltip("是否可交互")]
        [SerializeField] bool isInteractable = true;
        [Header("动画时间"), Tooltip("显示动画时间")]
        [SerializeField] protected float animTime = 0.3f;

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
            if (isAnimating)
            {
                StopCoroutine(ActiveAnimCoroutine);
            }

            if (isActive)
            {
                canvasGroup.alpha = 0;
                gameObject.SetActive(true);
            }

            StartCoroutine(Anim(isActive));
        }
        /// <summary>
        /// 设置UI显示并立即
        /// </summary>
        /// <param name="isActive"></param>
        public virtual void SetUIActiveImmediately(bool isActive)
        {
            if (isAnimating)
            {
                StopCoroutine(ActiveAnimCoroutine);
            }

            gameObject.SetActive(isActive);
            canvasGroup.alpha = isActive ? 1 : 0;

            if (isInteractable)
            {
                canvasGroup.interactable = isActive;
                canvasGroup.blocksRaycasts = isActive;
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
