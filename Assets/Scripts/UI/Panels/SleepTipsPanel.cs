using System.Collections;
using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class SleepTipsPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _tipText;
        [SerializeField] private float _autoHideDuration = 5f;

        private Coroutine _hideCoroutine;

        public void ShowSleepTip(string message)
        {
            if (_tipText != null)
                _tipText.text = message;

            SetUIActive(true);

            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(AutoHide());
        }

        private IEnumerator AutoHide()
        {
            yield return new WaitForSeconds(_autoHideDuration);
            SetUIActive(false);
        }

        public void Dismiss()
        {
            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);
            SetUIActive(false);
        }
    }
}
