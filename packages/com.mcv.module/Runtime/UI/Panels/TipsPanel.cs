using System.Collections;
using MCV_Module.UI.Components;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class TipsPanel : PanelBase
    {
        [SerializeField] TextComponent tipsText;
        [SerializeField] RectTransform moveRect;
        [SerializeField] float fadeInDuration = 0.5f;
        [SerializeField] float fadeOutDuration = 0.5f;
        [SerializeField] Vector2 movePosLimit = new Vector2(0, -100);
        CanvasGroup moveCanvasGroup;
        Coroutine fadeCoroutine;

        protected override void Awake()
        {
            base.Awake();
            if (tipsText == null || moveRect == null)
            {
                Debug.LogWarning($"[TipsPanel] 未找到 TextComponent 或 RectTransform：{name}", this);
                return;
            }
            
            moveCanvasGroup = moveRect.GetComponent<CanvasGroup>();

        }

        public void SetText(string text)
        {
            tipsText.SetContent(text);
            TextAnim(true);
        }

        void TextAnim(bool isIn)
        {
            if (isIn)
            {
                MoveIn();
            }
            else
            {
                MoveOut();
            }
        }

        void MoveIn()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeIn());
        }

        void MoveOut()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeOut());
        }

        IEnumerator FadeIn()
        {
            float time = 0;
            float currentAlpha = moveCanvasGroup.alpha;
            Vector2 currentPos = moveRect.anchoredPosition;
            Vector2 targetPos = new Vector2(movePosLimit.x, currentPos.y);

            while (time < fadeInDuration)
            {
                time += Time.deltaTime;
                moveCanvasGroup.alpha = Mathf.Lerp(currentAlpha, 1, time / fadeInDuration);
                moveRect.anchoredPosition = Vector2.Lerp(currentPos, targetPos, time / fadeInDuration);
                yield return null;
            }
            moveCanvasGroup.alpha = 1;
            moveRect.anchoredPosition = targetPos;

            fadeCoroutine = null;
            yield break;
        }

        IEnumerator FadeOut()
        {
            float time = 0;
            float currentAlpha = moveCanvasGroup.alpha;
            Vector2 currentPos = moveRect.anchoredPosition;
            Vector2 targetPos = new Vector2(movePosLimit.y, currentPos.y);

            while (time < fadeOutDuration)
            {
                time += Time.deltaTime;
                moveCanvasGroup.alpha = Mathf.Lerp(currentAlpha, 0, time / fadeOutDuration);
                moveRect.anchoredPosition = Vector2.Lerp(currentPos, targetPos, time / fadeOutDuration);
                yield return null;
            }

            moveCanvasGroup.alpha = 0;
            moveRect.anchoredPosition = targetPos;
            fadeCoroutine = null;
            yield break;
        }
    }
}
