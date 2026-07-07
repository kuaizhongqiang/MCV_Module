using TMPro;
using UnityEngine;

namespace MCV_Module.UI.Panels
{
    public class CopyrightPanel : PanelBase
    {
        [SerializeField] private TextMeshProUGUI _copyrightText;

        public void SetCopyright(string text)
        {
            if (_copyrightText != null)
                _copyrightText.text = text;
        }

        private void Start()
        {
            if (_copyrightText != null && string.IsNullOrEmpty(_copyrightText.text))
                _copyrightText.text = "Copyright © 2026 MCV_Module";
        }
    }
}
