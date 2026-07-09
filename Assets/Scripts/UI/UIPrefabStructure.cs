
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI
{
    public static class InputGroupHelper
    {
        public static void InputGroup(GameObject prefab,
            out TextMeshProUGUI label, out TMP_InputField inputField)
        {
            label = prefab.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            inputField = prefab.transform.GetChild(1).GetComponent<TMP_InputField>();
        }
    }

    public static class PanelBgHelper
    {
        public static void PanelBg(GameObject panel, 
            out Image colorBg, out Image frameColor)
        {
            colorBg = panel.transform.GetChild(0).GetComponent<Image>();
            frameColor = panel.transform.GetChild(1).GetChild(0).GetComponent<Image>();

        }
    }
}
