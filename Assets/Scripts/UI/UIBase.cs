
using System;
using System.Collections;
using UnityEngine;

namespace MCV_Module.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIBase : MonoBehaviour
    {
        protected CanvasGroup m_CanvasGroup;
        Coroutine m_Coroutine;
        [SerializeField] float duration = 0.3f;
        [SerializeField] bool isShowOnStart = true;
        public bool isAnimating = false;

        protected virtual void Awake()
        {
            m_CanvasGroup = GetComponent<CanvasGroup>();
            gameObject.SetActive(isShowOnStart);
            m_CanvasGroup.alpha = isShowOnStart ? 1 : 0;
            m_CanvasGroup.interactable = isShowOnStart;
            m_CanvasGroup.blocksRaycasts = isShowOnStart;
        }

        protected virtual void OnEnable()
        {
            
        }

        protected virtual void OnDisable()
        {
            
        }

        protected virtual void OnDestroy()
        {
            
        }

        public virtual void OnVisible(Action callback = null)
        {
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
                m_Coroutine = null;
            }
            m_Coroutine = StartCoroutine(ActiveAnimation(true, callback));
        }

        public virtual void OnInvisible(Action callback = null)
        {
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
                m_Coroutine = null;
            }
            m_Coroutine = StartCoroutine(ActiveAnimation(false, callback));
        }

        public virtual void SetUIActive(bool active)
        {
            if (active)
            {
                gameObject.SetActive(true);
                OnVisible();
            }
            else
            {
                OnInvisible(() =>
                {
                    gameObject.SetActive(false);
                });
            }
        }

        #region 工具方法
        protected void ClearChildren(Transform trans)
        {
            for (int i = trans.childCount - 1; i >= 0; i--)
            {
                Destroy(trans.GetChild(i).gameObject);
            }
        }
        #endregion

        #region 协程
        protected IEnumerator ActiveAnimation(bool active, Action callback = null)
        {
            float time = 0;
            float currentAlpha = m_CanvasGroup.alpha;
            float targetAlpha = active ? 1 : 0;
            isAnimating = true;
            while (time < duration)
            {
                time += Time.deltaTime;
                m_CanvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, time / duration);
                yield return null;
            }

            m_CanvasGroup.alpha = targetAlpha;

            callback?.Invoke();

            m_CanvasGroup.interactable = active;
            m_CanvasGroup.blocksRaycasts = active;

            m_Coroutine = null;
            isAnimating = false;
        }
        #endregion
    }
}