using MCV_Module.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    public class LoadingPanel : PanelBase
    {
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _progressText2;
        [SerializeField] private RawImage _oldScreenShot;
        [SerializeField] private RawImage _newScreenShot;

        protected override void Awake()
        {
            m_CanvasGroup.alpha = 0;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        protected override void OnEnable()
        {
            EventBus<SceneLoadingEvent>.Subscribe(OnSceneLoading);
        }

        protected override void OnDisable()
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

            if (evt.Progress >= 1f)
            {
                OnInvisible(() => gameObject.SetActive(false));
            }
        }

        private void UpdateProgress(float progress)
        {
            float clamped = Mathf.Clamp01(progress);

            if (_progressSlider != null)
                _progressSlider.value = clamped;

            if (_progressText != null)
                _progressText.text = $"加载中... {Mathf.FloorToInt(clamped * 100)}%";
            if (_progressText2 != null)
                _progressText2.text = $"加载中... {Mathf.FloorToInt(clamped * 100)}%";
            
        }
    }
}
