using System.Collections;
using MCV_Module.Managers;
using UnityEngine;
using MCV_Module.UI.Components;
using UnityEngine.UI;


namespace MCV_Module.UI.Tools
{
    // WHY: 本类不是 MonoBehaviour，协程借宿主(host)的 StartCoroutine 跑；host 为 null 时 SwitchContent 直接返回，宿主失活则动画静默中断。
    /// <summary>单条提示的进场/收起动画（alpha 淡入淡出 + X 位移）：支持文本模式(Text)与图片模式(RawImage)，宿主只借 StartCoroutine。</summary>
    public class TipsContentUtiliy
    {
        RectTransform obj;
        Transform parent;
        CanvasGroup canvasGroup;
        MonoBehaviour host;
        float moveDuration = 0.5f;
        Text text;         // 文字内容显示（TMP 形态下节点上的 Legacy 会被卸载 ⇒ 它随后成"假 null"）
        TextComponent textComp;   // 同节点的组件：换形态后读/写都必须经它（见构造函数里的 WHY）
        GameObject textGo;        // 文字节点本身：GameObject 在换组件后依然存活，SetActive 必须用它
        RawImage rawImage;            // 图片内容显示（TipImagePath 模式）
        // WHY: moveLimit.x = 打开位、moveLimit.y = 收起位，写反会让进场/收起方向颠倒。
        Vector2 moveLimit = new Vector2(0, 600);
        Coroutine moveCoroutine;

        public TipsContentUtiliy(Transform parent, GameObject obj, float moveDuration, MonoBehaviour host)
        {
            this.parent = parent;
            this.obj = obj.GetComponent<RectTransform>();
            canvasGroup = obj.GetComponent<CanvasGroup>();
            text = obj.GetComponentInChildren<Text>();
            // WHY: 组件必须在**构造时**就取好 —— TMP 形态下 TextComponent 会卸载该节点上的 Legacy Text，
            // 之后 text 成"假 null"、对它调 GetComponent 会抛；而组件本身一直存活。
            textComp = obj.GetComponentInChildren<TextComponent>();
            // WHY: 连文字节点本身也要缓存 —— text.gameObject 在换形态后同样是"假 null"，SetActive 会被静默跳过。
            textGo = textComp != null ? textComp.gameObject : (text != null ? text.gameObject : null);
            rawImage = obj.GetComponentInChildren<RawImage>();
            this.moveDuration = moveDuration;
            this.host = host;
        }

        public void SwitchContent(bool isOpen, System.Action onComplete = null)
        {
            if (host == null) return;

            if (moveCoroutine != null)
                host.StopCoroutine(moveCoroutine);

            if (isOpen)
                moveCoroutine = host.StartCoroutine(OpenContent(onComplete));
            else
                moveCoroutine = host.StartCoroutine(CloseContent(onComplete));
        }

        public void StopAnimation()
        {
            if (moveCoroutine != null && host != null)
            {
                host.StopCoroutine(moveCoroutine);
            }
            moveCoroutine = null;
        }

        public void SetPosState(bool isOpen)
        {
            float x = isOpen ? moveLimit.x : moveLimit.y;
            obj.anchoredPosition = new Vector2(x, obj.anchoredPosition.y);
            canvasGroup.alpha = isOpen ? 1 : 0;
        }

        public string GetCurrentContent() => textComp != null ? textComp.RawText : (text != null ? TextComponent.ReadRaw(text) : "");

        /// <summary> 设置文字内容，隐藏 RawImage（如有） </summary>
        public void SetContent(string content)
        {
            // WHY: 优先走组件 —— 换形态后 text 成"假 null"，静态入口 SetTextOn 会静默 no-op（提示正文根本写不进去）；
            // 中文排版（NBSP 缩进 + 标点避头）改由节点上的组件按 cjkTypography 开关负责。
            if (textComp != null) textComp.SetText(content);
            else if (text != null) TextComponent.SetTextOn(text, content);
            if (textGo != null) textGo.SetActive(true);
            if (rawImage != null)
            {
                rawImage.gameObject.SetActive(false);
            } 
        }

        /// <summary> 设置 AB 加载的图片，隐藏文字（如有） </summary>
        public void SetImage(Texture2D texture)
        {
            if (rawImage != null)
            {
                rawImage.texture = texture;
                rawImage.gameObject.SetActive(true);
                rawImage.SetNativeSize();
            }
            // WHY: 用缓存的节点而不是 text.gameObject —— 换形态后 text 是"假 null"，原写法会被静默跳过、显示图片时文字不隐藏。
            if (textGo != null) textGo.SetActive(false);
        }

        /// <summary> 从 AB 包异步加载图片并显示，失败时回退到 fallbackText </summary>
        public void LoadAndSetImage(string imageKey, string fallbackText = "")
        {
            if (string.IsNullOrEmpty(imageKey))
            {
                SetContent(fallbackText);
                return;
            }

            GlobalAddressableMgr.Instance.LoadAssetAsync<Texture2D>(imageKey, loadedTex =>
            {
                if (loadedTex != null)
                    SetImage(loadedTex);
                else if (!string.IsNullOrEmpty(fallbackText))
                    SetContent(fallbackText);
            });
        }

        IEnumerator OpenContent(System.Action onComplete = null)
        {
            float time = 0;
            float currentAlpha = canvasGroup.alpha;

            while (time < moveDuration)
            {
                time += Time.deltaTime;
                float t = time / moveDuration;
                canvasGroup.alpha = Mathf.Lerp(currentAlpha, 1, t);
                obj.anchoredPosition = new Vector2(Mathf.Lerp(moveLimit.y, moveLimit.x, t), obj.anchoredPosition.y);
                yield return null;
            }

            canvasGroup.alpha = 1;
            obj.anchoredPosition = new Vector2(moveLimit.x, obj.anchoredPosition.y);
            moveCoroutine = null;
            onComplete?.Invoke();
        }

        IEnumerator CloseContent(System.Action onComplete = null)
        {
            float time = 0;
            float currentAlpha = canvasGroup.alpha;

            while (time < moveDuration)
            {
                time += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(currentAlpha, 0, time / moveDuration);
                yield return null;
            }

            canvasGroup.alpha = 0;
            moveCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
