using System.Collections;
using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class TipsPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _tipText;
        [SerializeField] private float _defaultDuration = 3f;

        private Coroutine _hideCoroutine;

        public void ShowTip(string message, float duration = -1)
        {
            if (_tipText != null)
                _tipText.text = message;

            SetUIActive(true);

            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);

            if (duration < 0) duration = _defaultDuration;
            _hideCoroutine = StartCoroutine(HideAfterDelay(duration));
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetUIActive(false);
        }
    }
}
