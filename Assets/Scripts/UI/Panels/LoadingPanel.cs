using MCV_Module.Event;
using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class LoadingPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private RectTransform _progressBar;
        [SerializeField] private float _barMaxWidth = 300f;

        private void Awake()
        {
            // 确保初始隐藏
            m_CanvasGroup.alpha = 0;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus<SceneLoadingEvent>.Subscribe(OnSceneLoading);
        }

        private void OnDisable()
        {
            EventBus<SceneLoadingEvent>.Unsubscribe(OnSceneLoading);
        }

        private void OnSceneLoading(SceneLoadingEvent evt)
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
                OnVisible();
            }

            UpdateProgress(evt.Progress);

            // 加载完成
            if (evt.Progress >= 1f)
            {
                OnInvisible(() => gameObject.SetActive(false));
            }
        }

        private void UpdateProgress(float progress)
        {
            if (_progressText != null)
                _progressText.text = $"加载中... {Mathf.FloorToInt(progress * 100)}%";

            if (_progressBar != null)
            {
                var size = _progressBar.sizeDelta;
                size.x = _barMaxWidth * Mathf.Clamp01(progress);
                _progressBar.sizeDelta = size;
            }
        }
    }
}
